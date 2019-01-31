using System;
using System.IO;

namespace ImageTools {
    public class PatchInfo {
        public static PatchInfo Read(string fileName) {
            if (!File.Exists(fileName))
                return null;

            PatchInfo result = new PatchInfo();
            foreach (string line in File.ReadAllLines(fileName)) {
                string[] tokens = line.Split(':');
                if (tokens.Length < 2)
                    continue;

                string key = tokens[0].Trim();
                string value = tokens[1].Trim();
                switch (key) {
                    case "timeshift":
                        result.TimeShift = ParseTimeshift(value);
                        break;
                }
            }
            return result;
        }
        static TimeSpan ParseTimeshift(string value) {
            TimeSpan timeshift;
            if (TimeSpan.TryParse(value, out timeshift))
                return timeshift;
            else {
                string[] dates = value.Split('-');
                if (dates.Length == 2) {
                    DateTime start;
                    if (!DateTime.TryParse(dates[0].Trim(), out start))
                        return TimeSpan.Zero;
                    DateTime end;
                    if (!DateTime.TryParse(dates[1].Trim(), out end))
                        return TimeSpan.Zero;
                    return end - start;
                }
                return TimeSpan.Zero;
            }
        }

        public TimeSpan TimeShift { get; set; }
    }
}