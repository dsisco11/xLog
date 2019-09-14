namespace xLog.VirtualTerminal
{
    public struct TextChunk
    {
        public readonly int Offset;
        public readonly StringPtr Text;

        public TextChunk(int offset, StringPtr text)
        {
            Offset = offset;
            Text = text;
        }
    }
}
