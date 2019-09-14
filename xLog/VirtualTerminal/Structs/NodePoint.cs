using System.Runtime.CompilerServices;

namespace xLog.VirtualTerminal
{
    public enum ENodePosition
    {
        /// <summary>
        /// Set when other is preceding node.
        /// </summary>
        PRECEDING = 0x02,
        /// <summary>
        /// Set when other is following node.
        /// </summary>
        FOLLOWING = 0x04,
        /// <summary>
        /// Set when other is an ancestor of node.
        /// </summary>
        CONTAINS = 0x08,
        /// <summary>
        /// Set when other is a descendant of node.
        /// </summary>
        CONTAINED_BY = 0x10,
    };

    public struct NodePoint
    {
        public int StartOffset;
        public int EndOffset;

        public NodePoint(int startOffset, int endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ENodePosition Compare(NodePoint A, NodePoint B)
        {
            if (A.EndOffset < B.StartOffset)
            {
                return ENodePosition.FOLLOWING;
            }
            else if (A.StartOffset > B.EndOffset)
            {
                return ENodePosition.PRECEDING;
            }
            else if (A.StartOffset >= B.StartOffset && A.EndOffset <= B.EndOffset)
            {
                return ENodePosition.CONTAINS;
            }
            else if (A.StartOffset <= B.StartOffset && A.EndOffset > B.StartOffset)
            {
                return ENodePosition.CONTAINED_BY;
            }
            else
            {
                throw new System.NotImplementedException("Unhandled node position case");
            }
        }
    }
}
