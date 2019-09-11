using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace xLog
{
    /// <summary>
    /// Provides access to a genericized, consumable stream of data.
    /// </summary>
    /// <typeparam name="ItemType"></typeparam>
    internal class DataStream<ItemType>
    {
        #region Properties
        /// <summary>
        /// Our stream of tokens
        /// </summary>
        private readonly ReadOnlyMemory<ItemType> Data;
        private ReadOnlySpan<ItemType> Stream => Data.Span;

        /// <summary>
        /// The current position at which data will be read from the stream
        /// </summary>
        public ulong Position { get; private set; } = 0;

        public readonly ItemType EOF_ITEM = default(ItemType);
        #endregion

        #region Constructors
        public DataStream(ReadOnlyMemory<ItemType> Data, ItemType EOF_ITEM)
        {
            this.Data = Data;
            this.EOF_ITEM = EOF_ITEM;
        }

        public DataStream(ItemType[] Items, ItemType EOF_ITEM)
        {
            this.Data = new ReadOnlyMemory<ItemType>(Items);
            this.EOF_ITEM = EOF_ITEM;
        }
        #endregion

        #region Accessors
        public int Length => Data.Length;
        public ulong LongLength => (ulong)Data.Length;
        public ulong Remaining => ((ulong)Data.Length - Position);
        /// <summary>
        /// Returns the next item to be consumed, equivalent to calling Peek(0)
        /// </summary>
        public ItemType Next => Peek(0);
        /// <summary>
        /// Returns the next item to be consumed, equivalent to calling Peek(1)
        /// </summary>
        public ItemType NextNext => Peek(1);
        /// <summary>
        /// Returns the next item to be consumed, equivalent to calling Peek(2)
        /// </summary>
        public ItemType NextNextNext => Peek(2);

        /// <summary>
        /// Returns whether the stream position is currently at the end of the stream
        /// </summary>
        public bool atEnd => Position >= LongLength;

        /// <summary>
        /// Returns whether the next character in the stream is the EOF character
        /// </summary>
        public bool atEOF => Peek(0).Equals(EOF_ITEM);
        #endregion

        #region Data
        /// <summary>
        /// Direct accessor to the Data <see cref="Memory{T}"/> instance
        /// </summary>
        public ReadOnlyMemory<ItemType> AsMemory() => Data;
        /// <summary>
        /// Direct accessor to the Data <see cref="Memory{T}"/> instances' span
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<ItemType> AsSpan() => Data.Span;
        #endregion

        #region Seeking
        /// <summary>
        /// Seeks to a specific position in the stream
        /// </summary>
        /// <param name="position"></param>
        public void Seek(ulong position, bool end = false)
        {
            if (end)
            {
                Position = ((ulong)Length - position);
            }
            else
            {
                Position = position;
            }
        }
        #endregion

        #region Peeking
        /// <summary>
        /// Returns the item at +<paramref name="Offset"/> from the current read position
        /// </summary>
        /// <param name="Offset">Distance from the current read position at which to peek</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemType Peek(long Offset = 0)
        {
            long index = ((long)Position + Offset);

            if (index < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (index >= Stream.Length)
            {
                return EOF_ITEM;
            }

            return Stream[(int)index];
        }

        /// <summary>
        /// Returns the item at +<paramref name="Offset"/> from the current read position
        /// </summary>
        /// <param name="Offset">Distance from the current read position at which to peek</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ItemType Peek(ulong Offset = 0)
        {
            ulong index = (Position + Offset);

            if (index < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (index >= (ulong)Stream.Length)
            {
                return EOF_ITEM;
            }

            return Stream[(int)index];
        }
        #endregion

        #region Find
        /// <summary>
        /// Returns the index of the first item matching the given <paramref name="subject"/>  or -1 if none was found
        /// </summary>
        /// <returns>Index of first item matching the given one or -1 if none was found</returns>
        public bool Scan(ItemType subject, out ulong outOffset, ulong startOffset = 0, IEqualityComparer<ItemType> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<ItemType>.Default;
            ulong offset = startOffset;

            while ((offset + Position) < (ulong)Length)
            {
                var current = Peek(offset);
                if (comparer.Equals(current, subject))
                {
                    outOffset = offset;
                    return true;
                }

                offset++;
            }

            outOffset = 0;
            return false;
        }

        /// <summary>
        /// Returns the index of the first item matching the given predicate or -1 if none was found
        /// </summary>
        /// <returns>Index of first item matching the given predicate or -1 if none was found</returns>
        public bool Scan(Predicate<ItemType> Predicate, out ulong outOffset, ulong startOffset = 0)
        {
            ulong offset = startOffset;

            while ((offset + Position) < (ulong)Length)
            {
                var current = Peek(offset);
                if (Predicate(current))
                {
                    outOffset = offset;
                    return true;
                }

                offset++;
            }

            outOffset = 0;
            return false;
        }
        #endregion

        #region Consume
        /// <summary>
        /// Returns the first unconsumed item from the stream and progresses the current reading position
        /// </summary>
        public ItemType Consume()
        {
            var EndPos = (Position + 1);
            if (Position >= (ulong)Stream.Length) return EOF_ITEM;

            ItemType retVal = Stream[(int)Position];
            Position += 1;

            return retVal;
        }

        /// <summary>
        /// Returns the first unconsumed item from the stream and progresses the current reading position
        /// </summary>
        public CastType Consume<CastType>() where CastType : ItemType
        {
            var EndPos = (Position + 1);
            if (Position >= (ulong)Stream.Length) return default(CastType);

            ItemType retVal = Stream[(int)Position];
            Position += 1;

            return (CastType)retVal;
        }

        /// <summary>
        /// Returns the specified number of items from the stream and progresses the current reading position by that number
        /// </summary>
        /// <param name="Count">Number of characters to consume</param>
        public ReadOnlySpan<ItemType> Consume(ulong Count = 1)
        {
            var startIndex = Position;
            var endIndex = (Position + Count);

            if (endIndex >= (ulong)Stream.Length)
            {
                endIndex = (((ulong)Stream.Length) - 1);
            }

            Position = endIndex;
            return Stream.Slice((int)startIndex, (int)Count);
        }

        /// <summary>
        /// Consumes items until reaching the first one that does not match the given predicate, then returns all matched items and progresses the current reading position by that number
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns>True if atleast a single item was consumed</returns>
        public bool Consume_While(Predicate<ItemType> Predicate, int? limit = null)
        {
            bool consumed = Predicate(Next);
            while (Predicate(Next) && (!limit.HasValue || limit.Value >= 0) && !atEnd)
            {
                if (limit.HasValue) limit--;
                Consume();
            }

            return consumed;
        }

        /// <summary>
        /// Consumes items until reaching the first one that does not match the given predicate, then returns all matched items and progresses the current reading position by that number
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns>True if atleast a single item was consumed</returns>
        public bool Consume_While(Predicate<ItemType> Predicate, out ReadOnlyMemory<ItemType> outConsumed, int? limit = null)
        {
            var startIndex = Position;

            while (Predicate(Next) && (!limit.HasValue || limit.Value >= 0) && !atEnd)
            {
                if (limit.HasValue) limit--;
                Consume();
            }

            var count = Position - startIndex;
            outConsumed = Data.Slice((int)startIndex, (int)count);
            return count > 0;
        }

        /// <summary>
        /// Consumes items until reaching the first one that does not match the given predicate, then returns all matched items and progresses the current reading position by that number
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns>True if atleast a single item was consumed</returns>
        public bool Consume_While(Predicate<ItemType> Predicate, out ReadOnlySpan<ItemType> outConsumed, int? limit = null)
        {
            var startIndex = Position;

            while (Predicate(Next) && (!limit.HasValue || limit.Value >= 0) && !atEnd)
            {
                if (limit.HasValue) limit--;
                Consume();
            }

            var count = Position - startIndex;
            outConsumed = Stream.Slice((int)startIndex, (int)count);
            return count > 0;
        }

        /// <summary>
        /// Pushes the given number of items back onto the front of the stream
        /// </summary>
        /// <param name="Count"></param>
        public void Reconsume(long Count = 1)
        {
            if ((ulong)Count > Position) throw new ArgumentOutOfRangeException($"{nameof(Count)} exceeds the number of items consumed.");

            Position -= (ulong)Count;
        }

        /// <summary>
        /// Pushes the given number of items back onto the front of the stream
        /// </summary>
        /// <param name="Count"></param>
        public void Reconsume(ulong Count = 1)
        {
            if (Count > Position) throw new ArgumentOutOfRangeException($"{nameof(Count)} exceeds the number of items consumed.");
            Position -= Count;
        }

        #endregion

        #region SubStream

        /// <summary>
        /// Consumes the number of items specified by <paramref name="Count"/> and then returns them as a new stream, progressing this streams reading position to the end of the consumed items
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public DataStream<ItemType> Substream(ulong Count)
        {
            if (Count > Remaining) throw new ArgumentOutOfRangeException($"{nameof(Count)} exceeds the number of remaining items.");
            var consumed = Data.Slice((int)Position, (int)Count);
            Position += Count;
            return new DataStream<ItemType>(consumed, EOF_ITEM);
        }

        /// <summary>
        /// Consumes the number of items specified by <paramref name="Count"/> and then returns them as a new stream, progressing this streams reading position to the end of the consumed items
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public DataStream<ItemType> Substream(ulong offset = 0, ulong? Count = null)
        {
            if (!Count.HasValue)
            {
                Count = (ulong)Length - (Position + offset);
            }

            if (Count > Remaining) throw new ArgumentOutOfRangeException($"{nameof(Count)} exceeds the number of remaining items.");

            Position += offset;
            var consumed = Data.Slice((int)Position, (int)Count.Value);
            Position += Count.Value;
            return new DataStream<ItemType>(consumed, EOF_ITEM);
        }

        /// <summary>
        /// Consumes items until reaching the first one that does not match the given <paramref name="Predicate"/>, progressing this streams reading position by that number and then returning all matched items as new stream
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public DataStream<ItemType> Substream(Predicate<ItemType> Predicate)
        {
            var startIndex = Position;

            while (Predicate(Next))
            {
                Consume();
            }

            var count = Position - startIndex;
            var consumed = Data.Slice((int)startIndex, (int)count);

            return new DataStream<ItemType>(consumed, EOF_ITEM);
        }
        #endregion

        #region Slicing
        /// <summary>
        /// Returns a slice of this streams memory containing all of the data after current stream position + <paramref name="offset"/>
        /// </summary>
        /// <param name="offset">Offset from the current stream position where the memory slice to begin</param>
        /// <returns></returns>
        public ReadOnlyMemory<ItemType> Slice(ulong offset = 0)
        {
            var index = Math.Max(LongLength, Position + offset);
            return Data.Slice((int)index, (int)(LongLength - index));
        }

        /// <summary>
        /// Returns a slice of this streams memory containing all of the data after current stream position + <paramref name="offset"/>
        /// </summary>
        /// <param name="offset">Offset from the current stream position where the memory slice to begin</param>
        /// <param name="count">The number of items to include in the slice</param>
        /// <returns></returns>
        public ReadOnlyMemory<ItemType> Slice(ulong offset, ulong count)
        {
            var index = Math.Max(LongLength, Position + offset);
            return Data.Slice((int)index, (int)(LongLength - index));
        }
        #endregion

        #region Cloning
        /// <summary>
        /// Creates and returns a copy of this stream
        /// </summary>
        /// <returns></returns>
        public DataStream<ItemType> Clone()
        {
            return new DataStream<ItemType>(Data, EOF_ITEM) { Position = this.Position };
        }
        #endregion
    }
}
