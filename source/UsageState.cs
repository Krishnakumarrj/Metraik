using System;
using System.Collections.Generic;

namespace Metraik
{
    // Provider labels and allowlisted browser destinations; these are not account connections.
    internal static class ProviderCatalog
    {
        public static readonly string[] Names = { "ChatGPT", "Claude", "Gemini", "Cursor", "Copilot" };
        public static string Normalise(string name)
        {
            return Array.IndexOf(Names, name) >= 0 ? name : "ChatGPT";
        }
        public static string Url(string name)
        {
            switch (name)
            {
                case "Claude": return "https://claude.ai/";
                case "Gemini": return "https://gemini.google.com/";
                case "Cursor": return "https://cursor.com/";
                case "Copilot": return "https://copilot.microsoft.com/";
                default: return "https://chatgpt.com/";
            }
        }
    }
    internal static class UsageState
    {
        public static bool ActiveKnown(AppSettings s, DateTime now)
        {
            return s.IsDemo || (s.LastEditedLocal <= now.AddMinutes(5) && s.ActiveResetLocal > now);
        }
        public static bool WeeklyKnown(AppSettings s, DateTime now)
        {
            return s.IsDemo || (s.LastEditedLocal <= now.AddMinutes(5) && s.WeeklyResetLocal > now);
        }
        public static string Countdown(DateTime reset, DateTime now)
        {
            TimeSpan t = reset - now;
            if (t.TotalSeconds <= 0) return "passed";
            if (t.TotalDays >= 1) return String.Format("{0}d {1}h", (int)t.TotalDays, t.Hours);
            if (t.TotalHours >= 1) return String.Format("{0}h {1}m", (int)t.TotalHours, t.Minutes);
            return String.Format("{0}m", Math.Max(1, (int)Math.Ceiling(t.TotalMinutes)));
        }
    }
    internal sealed class AlertTracker
    {
        private readonly HashSet<string> notified = new HashSet<string>();
        public string Evaluate(AppSettings s, DateTime now)
        {
            if (s.IsDemo || !s.AlertsEnabled) return null;
            List<string> low = new List<string>();
            Check(low, s, "Active", s.ActiveRemaining, s.ActiveResetLocal, UsageState.ActiveKnown(s, now));
            Check(low, s, "Weekly", s.WeeklyRemaining, s.WeeklyResetLocal, UsageState.WeeklyKnown(s, now));
            return low.Count == 0 ? null : s.Provider + " manual reading: " + String.Join("; ", low.ToArray());
        }
        private void Check(List<string> low, AppSettings s, string window, int percent, DateTime reset, bool known)
        {
            if (!known || percent > s.AlertThreshold) return;
            string key = s.Provider + ":" + window + ":" + reset.Ticks;
            if (notified.Add(key)) low.Add(window + " " + percent + "% remaining");
        }
    }
}
