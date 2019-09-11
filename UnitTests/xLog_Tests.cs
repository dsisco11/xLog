using System;
using Xunit;
using xLog;

namespace UnitTests
{
    public class xLog_Tests
    {
        [Fact]
        public void Test_VT_Coloring()
        {
            string Text = "Hello World!";
            var cText = xLog.ANSI.White(Text);
            Assert.NotEqual(Text, cText);
        }

        [Fact]
        public void Test_VT_Color_Stripping()
        {
            string Text = "[39mHello World!";
            var cText = xLog.ANSI.Strip(Text.AsMemory());
            Assert.NotEqual(Text, cText);
        }
    }
}
