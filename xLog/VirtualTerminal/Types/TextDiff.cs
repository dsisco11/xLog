using System.Threading;
using System.Threading.Tasks;
using xLog.Types;

namespace xLog.VirtualTerminal
{
    public enum EDiffType
    {
        /// <summary>
        /// New data was added, total data length might increase
        /// </summary>
        Insertion,
        /// <summary>
        /// Existing data was removed, total data length might decrease
        /// </summary>
        Removal,
        /// <summary>
        /// Existing data was altered in place, total data length is unchanged
        /// </summary>
        Mutation
    };

    public class TextDiff
    {
        #region Properties
        public int Start, End;
        public EDiffType Type;
        #endregion

        #region Accessors
        public int Length => End - Start;
        #endregion

        #region Constructors
        public TextDiff(int start, int end, EDiffType type)
        {
            Start = start;
            End = end;
            Type = type;
        }
        #endregion


        #region Differencing
        public static TextDiff Consume_Diff(DataStream<char> Old, DataStream<char> New)
        {
            /* Scan both streams until we either find a spot ahead in one that matches the current spot in the other OR we hit the end of the stream */
            var CancelToken = new CancellationTokenSource();
            var uOld = Find_Next_Unique(Old, Old.Position);
            var uNew = Find_Next_Unique(New, New.Position);
            TextDiff addDiff = null;
            TextDiff rmvDiff = null;

            Parallel.Invoke(new ParallelOptions() { CancellationToken = CancelToken.Token },
                () =>/* Scan for addition */
                {/* Find the next unique in stream B that matches A's current */
                    if (uOld == null)
                    {
                        addDiff = new TextDiff(Old.Length, New.Length, EDiffType.Insertion);/* Insertion at end */
                        return;
                    }
                    if (uNew == null) return;
                    var index = New.Position + (uNew.Length-1);
                    while (index < New.Length)
                    {
                        var unique = Find_Next_Unique(New, index);
                        if (unique == null)
                        {
                            break;
                        }
                        else if (uOld.AsMemory().Equals(unique.AsMemory()))
                        {
                            addDiff = new TextDiff(New.Position, index, EDiffType.Insertion);
                            CancelToken.Cancel();
                        }
                        else
                        {
                            index += unique.Length-1;
                        }
                    }
                },
                () =>/* Scan for removal */
                {/* Find next unique in stream A matching B's current */
                    if (uNew == null)
                    {
                        rmvDiff = new TextDiff(New.Length, Old.Length, EDiffType.Removal);/* Removal at end */
                        return;
                    }
                    if (uOld == null) return;
                    var index = Old.Position + (uOld.Length - 1);

                    while (index < Old.Length)
                    {
                        var unique = Find_Next_Unique(Old, index);
                        if (unique == null)
                        {
                            break;
                        }
                        else if (uNew.AsMemory().Equals(unique.AsMemory()))
                        {
                            rmvDiff = new TextDiff(Old.Position, index, EDiffType.Insertion);
                            CancelToken.Cancel();
                        }
                        else
                        {
                            index += unique.Length-1;
                        }
                    }
                }
            );

            /* If nothing was found then the change must be a pure modification so we lockstep ahead in both streams to find the next spot that they both match */
            if (addDiff == null && rmvDiff == null)
            {
                int pos = New.Position;
                while ((pos+1) < Old.Length && (pos+1) < New.Length && Old.Peek(pos) != New.Peek(pos))
                {
                    pos++;
                }

                return new TextDiff(New.Position, pos, EDiffType.Mutation);
            }
            else if (addDiff != null && rmvDiff != null)/* Else find the shorter of the two diffs and return that one */
            {
                if (addDiff.End < rmvDiff.End)
                {
                    return addDiff;
                }
                else
                {
                    return rmvDiff;
                }
            }
            else if (addDiff != null)
            {
                return addDiff;
            }
            else if (rmvDiff != null)
            {
                return rmvDiff;
            }

            return null;
        }

        /// <summary>
        /// Finds the next unique structure in the given stream starting from the specified index
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="Start"></param>
        private static StringPtr Find_Next_Unique(DataStream<char> Stream, int Start)
        {
            if (Stream.Length == 0)
            {
                return null;
            }

            /* A 'unique' structure is simply the next word + whitespace + non-whitespace */
            bool bWord = false;
            bool bWhitespace = false;
            var pos = Start;
            while (pos < Stream.Length)
            {
                if (!char.IsWhiteSpace(Stream.Peek(pos)))
                {
                    if (!bWord)/* This is just the start of our word segment */
                    {
                        bWord = true;
                    }
                    else if (bWhitespace)/* We already have a word and whitespace so this is the starting char of the next word. this is where we end! */
                    {
                        return Stream.Slice(Start, 1+(pos - Start));
                    }
                }
                else
                {
                    if (bWord)/* the whitespace section doesnt start until we have a word */
                    {
                        if (!bWhitespace)
                            bWhitespace = true;
                    }
                }

                pos++;
            }

            /*if (pos == Stream.LongLength)
            {
                return Stream.Slice(Start, 1 + (pos - Start));
            }*/

            return null;
        }
        #endregion

        #region Overrides
        public override string ToString() => $"{Type}<{Start}, {End}>";
        #endregion
    }
}
