using System;
using System.Linq;
using System.Collections.Generic;

internal static class TimeSpanExtensions
{
    public static string ToHumanString(this TimeSpan span)
    {
        List<string> list = new List<string>();
        list.Add(span.Duration().Days > 0 ? string.Format("{0:0} day{1}", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty);
        list.Add(span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty);
        list.Add(span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty);
        list.Add(span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty);

        string formatted = string.Join(", ", list.Where(str => !string.IsNullOrWhiteSpace(str)));
        if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

        return formatted;
    }
}
