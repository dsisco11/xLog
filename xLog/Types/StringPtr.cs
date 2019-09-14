using System;

namespace xLog
{
    /// <summary>
    /// Faster string
    /// </summary>
    public class StringPtr
    {
        #region Properties
        public readonly ReadOnlyMemory<char> Data;
        #endregion

        #region Constructors
        public StringPtr(ReadOnlyMemory<char> memory)
        {
            Data = memory;
        }
        #endregion

        #region Implicit
        public static implicit operator StringPtr(string str) => new StringPtr(str.AsMemory());
        public static implicit operator StringPtr(ReadOnlyMemory<char> memory) => new StringPtr(memory);
        public static implicit operator ReadOnlyMemory<char>(StringPtr atom) => atom.Data;
        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            if (obj is StringPtr ptr)
            {
                return Data.Equals(ptr.Data);
            }
            else if (obj is ReadOnlyMemory<char> mem)
            {
                return Data.Equals(mem);
            }

            return base.Equals(obj);
        }

        public override string ToString() => Data.ToString();
        #endregion

        public ReadOnlyMemory<char> AsMemory() => Data;
        public ReadOnlySpan<char> AsSpan() => Data.Span;
        public int Length => Data.Length;

    }
}
