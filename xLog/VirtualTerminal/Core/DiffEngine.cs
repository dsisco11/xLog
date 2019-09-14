using System;
using System.Collections.Generic;
using xLog.Types;

namespace xLog.VirtualTerminal
{
    public static class DiffEngine
    {
        /// <summary>
        /// Compiles a list of positions and text to write at said positions
        /// </summary>
        /// <returns></returns>
        public static LinkedList<TextChunk> Compile_Transformations(StringPtr OldText, StringPtr NewText)
        {
            /* Get all of the places where we have changed */
            LinkedList<TextDiff> Diffs = Difference(OldText, NewText);

            /* Compile a list of all our CSI command blocks */
            LinkedList<CSI_BLOCK> Blocks = Terminal.Compile_Command_Blocks(NewText);

            /* Filter out any blocks which have no changes in them  */
            LinkedList<CSI_BLOCK> Changed = new LinkedList<CSI_BLOCK>();
            LinkedListNode<CSI_BLOCK> cBlock = Blocks.First;

            foreach (TextDiff diff in Diffs)
            {
                NodePoint dPos = new NodePoint(diff.Start, diff.End);
                if (cBlock == null) break;
                while (cBlock != null)
                {
                    if (!cBlock.Value.CmdByte.HasValue)
                    {/* Skip blocks which do not have a command */
                        cBlock = cBlock.Next;
                        break;
                    }

                    NodePoint bPos = new NodePoint(cBlock.Value.BlockStart, cBlock.Value.BlockEnd);
                    ENodePosition Pos = NodePoint.Compare(dPos, bPos);
                    if (Pos == ENodePosition.CONTAINS)
                    {/* This block collides with the current diff, add it to the changed blocks if it isnt already there and then continue to the next diff */
                        if (!ReferenceEquals(cBlock, Changed.Last))
                        {
                            Changed.AddLast(cBlock.Value);
                        }
                        break;
                    }
                    else
                    {/* This block occurs before/after the current diff, move on to the next block */
                        cBlock = cBlock.Next;
                        if (cBlock == null)
                            break;
                    }

                }
            }

            /* We want a series of commands that ONLY replace text which has changed BUT which does not skip CSI commands that may apply to those differences AND which do not include redundant CSI commands */
            int Balance = 0;// Tracks the total balance of text written vs text removed
            var RetList = new LinkedList<TextChunk>();
            LinkedListNode<TextDiff> cDiff = Diffs.First;
            cBlock = Changed.First;

            foreach (TextDiff diff in Diffs)
            {
                NodePoint dPos = new NodePoint(diff.Start, diff.End);
                /* Find the Command block(if any) that goes before this diff text  */
                bool bSearching = true;
                while (bSearching && cBlock != null)
                {
                    var block = cBlock.Value;
                    NodePoint bPos = new NodePoint(block.BlockStart, block.BlockEnd);
                    ENodePosition Pos = NodePoint.Compare(dPos, bPos);
                    switch (Pos)
                    {
                        case ENodePosition.PRECEDING:
                            {/* move to next block */
                                cBlock = cBlock.Next;
                            }
                            break;
                        case ENodePosition.CONTAINS:
                            {
                                bSearching = false;
                                cBlock = cBlock.Next;
                                RetList.AddLast(new TextChunk(block.BlockStart, NewText.AsMemory().Slice(block.BlockStart, block.CmdLength)));
                            }
                            break;
                        case ENodePosition.FOLLOWING:
                        case ENodePosition.CONTAINED_BY:
                        default:
                            {
                                bSearching = false;
                            }
                            break;
                    }
                }
                /* In order to account for the total changes which have occured we need to ensure that any text which will be overwritten which isnt supposed to be replaced will correctly be reprinted after the overwriting text */
                switch (diff.Type)
                {
                    case EDiffType.Mutation:
                        {
                            //Delta += diff.Length;/* This text was only altered, it should be invisible to our balance */
                            RetList.AddLast(new TextChunk(diff.Start, NewText.AsMemory().Slice(diff.Start, diff.Length)));
                        }
                        break;
                    case EDiffType.Insertion:
                        {/* In the case of inserted text we should insert all the text that WAS at the start of this diff but before the next as a command so it is reprinted */

                            RetList.AddLast(new TextChunk(diff.Start, NewText.AsMemory().Slice(diff.Start, diff.Length)));
                            Balance += diff.Length;
                            var next = cDiff.Next;
                            if (next != null)
                            {
                                var repeat_length = next.Value.Start - diff.Start;/* The length of text we need to repeat is whatever was between our insertion pos and the start of the next diff */
                                //Delta -= available;/* This text was already present, it should be invisible to our balance */
                                RetList.AddLast(new TextChunk(diff.End, NewText.AsMemory().Slice(diff.End, repeat_length)));/* We read from this diff's end because in the new buffer the repeated text is after this diff */
                            }
                            else/* No next diff, insert all needed text */
                            {
                                var repeat_length = (OldText?.Length ?? 0) - diff.Start;
                                RetList.AddLast(new TextChunk(diff.End, NewText.AsMemory().Slice(diff.End, repeat_length)));/* We read from this diff's end because in the new buffer the repeated text is after this diff */
                            }
                        }
                        break;
                    case EDiffType.Removal:
                        {/* For removals we just want to track how much text was removed */
                            Balance -= diff.Length;
                        }
                        break;
                }
            }

            /* If our balance is negative then we need to erase that many characters after the end of our buffer onscreen */
            if (Balance < 0)
            {
                RetList.AddLast(new TextChunk(NewText.Length, new string('\0', -Balance)));
            }

            return RetList;
        }

        /// <summary>
        /// Compiles a list of differences between the current text and some given text
        /// </summary>
        /// <param name="NewText"></param>
        /// <returns>List of start/end ranges</returns>
        public static LinkedList<TextDiff> Difference(StringPtr OldText, StringPtr NewText)
        {
            if ((OldText == null || OldText.Length <= 0) && (NewText == null || NewText.Length <= 0))
            {
                return new LinkedList<TextDiff>();
            }
            else if ((OldText == null || OldText.Length <= 0) && NewText.Length > 0)
            {
                return new LinkedList<TextDiff>(new TextDiff[] { new TextDiff(0, NewText.Length, EDiffType.Insertion) });
            }
            else if ((NewText == null || NewText.Length <= 0) && OldText.Length > 0)
            {
                return new LinkedList<TextDiff>(new TextDiff[] { new TextDiff(0, OldText.Length, EDiffType.Insertion) });
            }


            var A = new DataStream<char>(OldText.AsMemory(), '\0');
            var B = new DataStream<char>(NewText.AsMemory(), '\0');

            var Chunks = new LinkedList<TextDiff>();
            while (true)
            {
                if (A.Next != B.Next)/* Just entered a spot where the texts stopped matching */
                {
                    var diff = TextDiff.Consume_Diff(A, B);
                    if (diff == null || diff.Length <= 0)
                    {/* No more diffs available */
                        return Chunks;
                    }
                    /* Progress the spplicable stream by the difference ammount */
                    switch (diff.Type)
                    {
                        case EDiffType.Insertion:
                            {
                                B.Consume(diff.Length);
                            }
                            break;
                        case EDiffType.Removal:
                            {
                                A.Consume(diff.Length);
                            }
                            break;
                        case EDiffType.Mutation:
                            {
                                A.Consume(diff.Length);
                                B.Consume(diff.Length);
                            }
                            break;
                        default:
                            throw new NotImplementedException($"Handling for {nameof(EDiffType)} \"{diff.Type}\" has not been implemented!");
                    }

                    Chunks.AddLast(diff);
                }

                if (A.atEnd && B.atEnd)
                    break;
            }

            /* Check for text diff past the end of stream */

            return Chunks;
        }
    }
}
