
namespace xLog
{
    internal enum ANSI_CODE : int
    {
        /// <summary>
        /// Resets all active styling commands
        /// </summary>
        RESET_STYLE = 0,
        BOLD = 1,
        ITALIC = 3,
        UNDERLINE = 4,
        BLINK_SLOW = 5,
        BLINK_FAST = 6,
        INVERT = 7,

        SET_DEFAULT_FG = 10,
        SET_DEFAULT_BG = 11,

        BOLD_OFF = 22,
        ITALIC_OFF = 23,
        UNDERLINE_OFF = 24,
        BLINK_OFF = 25,
        INVERT_OFF = 26,

        SET_COLOR_FG = 30,
        SET_COLOR_BG = 40,

        SET_COLOR_CUSTOM_FG = 38,
        SET_COLOR_CUSTOM_BG = 48,

        RESET_COLOR_FG = 39,
        RESET_COLOR_BG = 49,

        SET_COLOR_FG_BRIGHT = 90,
        SET_COLOR_BG_BRIGHT = 100,
    }
}