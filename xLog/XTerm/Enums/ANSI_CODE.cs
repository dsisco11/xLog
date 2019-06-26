
internal enum ANSI_CODE : uint
{
    /// <summary>
    /// Resets all active styling commands
    /// <para>To reset just Bold+Italic see: <see cref="NORMAL"/></para>
    /// </summary>
    RESET_STYLE = 0,
    BOLD = 1,
    ITALIC = 3,
    BLINK_SLOW = 5,
    BLINK_FAST = 6,

    SET_DEFAULT_FG = 10,
    SET_DEFAULT_BG = 11,
    /// <summary>
    /// Neither Bold nor Italic
    /// </summary>
    NORMAL = 22,

    SET_COLOR_FG = 30,
    SET_COLOR_BG = 40,
    SET_COLOR_CUSTOM_FG = 38,
    SET_COLOR_CUSTOM_BG = 48,
    RESET_COLOR_FG = 39,
    RESET_COLOR_BG = 49,
    SET_COLOR_FG_BRIGHT = 90,
    SET_COLOR_BG_BRIGHT = 100,
}