using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
namespace ImageTools {
    public static class ImagePathBuilder {
        public static string BuildPath(string fileName, DateTime date, string rules) {
            char separator = Path.DirectorySeparatorChar;
            StringBuilder result = new StringBuilder();
            List<Token> tokens = PathRulesParser.Parse(rules);
            int count = tokens.Count;
            for (int i = 0; i < count; i++) {
                Token token = tokens[i];
                while (token != null) {
                    if (!String.IsNullOrEmpty(token.Filter)) {
                        string[] extensions = token.Filter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        string extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
                        if (Array.IndexOf<string>(extensions, extension) >= 0)
                            result.Append(separator + date.ToString(token.DisplayFormat));
                    }
                    else if ((!String.IsNullOrEmpty(token.ConditionFormat) && IsText(token.ConditionFormat)) || (String.IsNullOrEmpty(token.ConditionFormat) && IsText(token.DisplayFormat))) {
                        string condition = String.IsNullOrEmpty(token.ConditionFormat) ? token.DisplayFormat : token.ConditionFormat;
                        condition = condition.Trim('\'');
                        string display = token.DisplayFormat;

                        string directory = Path.GetDirectoryName(fileName);
                        string[] items = directory.Split(separator);
                        int index = Array.FindIndex<string>(items, (item) => {
                            return String.Equals(item, condition, StringComparison.OrdinalIgnoreCase);
                        });
                        if (index >= 0) {
                            if (index < items.Length - 1) {
                                index++;
                                string subDirectories = String.Join(separator.ToString(), items, index, items.Length - index);
                                result.Append(subDirectories);
                            }
                            return Path.Combine(result.ToString(), date.ToString(display), Path.GetFileName(fileName));
                        }
                    }
                    else {
                        if (token.ConditionFormat != null) {
                            string dateString = date.ToString(token.ConditionFormat, CultureInfo.InvariantCulture);
                            string[] dates = dateString.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                            if (dates.Length == 1) {
                                DateTime directoryDate;
                                if (DateTime.TryParse(dateString, out directoryDate) && DateTime.Equals(date.Date, directoryDate)) {
                                    result.Append(separator + date.ToString(token.DisplayFormat));
                                    break;
                                }
                            }
                            else {
                                DateTime startDate;
                                DateTime endDate;
                                if (DateTime.TryParse(dates[0].Trim(), out startDate) && DateTime.TryParse(dates[1].Trim(), out endDate)) {
                                    if (date.Date >= startDate && date.Date <= endDate) {
                                        result.Append(separator + date.ToString(token.DisplayFormat));
                                        break;
                                    }
                                }
                            }
                        }
                        else {
                            result.Append(separator + date.ToString(token.DisplayFormat));
                            break;
                        }
                    }
                    token = token.Next;
                }
            }
            return Path.Combine(result.ToString(), Path.GetFileName(fileName));
        }
        static bool IsText(string format) {
            return !String.IsNullOrEmpty(format) && format.StartsWith("\"") && format.EndsWith("\"");
        }
        public static string BuildPath(string outputDirectory, string file, string rules) {
            DateTime dateTime = GetCreationDate(file);
            string imagePath = outputDirectory.TrimEnd(Path.DirectorySeparatorChar) + BuildPath(file, dateTime, rules);
            if (File.Exists(imagePath) && dateTime != GetCreationDate(imagePath)) {
                string name = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file);
                string imagePathFormat = Path.Combine(Path.GetDirectoryName(imagePath), name + "_{0}" + extension);
                int suffix = 1;
                do {
                    imagePath = String.Format(imagePathFormat, suffix);
                    suffix++;
                } while (File.Exists(imagePath));
            }
            return imagePath;
        }
        static DateTime GetCreationDate(string file) {
            DateTime? date = GetImageDateTimeCreation(file);
            if (date != null)
                return date.Value;
            DateTime creationTime = File.GetCreationTime(file);
            DateTime lastWriteTime = File.GetLastWriteTime(file);
            if (creationTime < lastWriteTime)
                return creationTime;
            else
                return lastWriteTime;
        }
        static DateTime? GetImageDateTimeCreation(string path) {
            try {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    ExifReader reader = new ExifReader(stream);
                    Dictionary<ImagePropertyId, object> metadata = reader.ReadMetadata();
                    object value;
                    if (!metadata.TryGetValue(ImagePropertyId.DateTime, out value))
                        return null;

                    string dateTime = ((string)value).TrimEnd('\0');
                    string[] tokens = dateTime.Split(new char[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length >= 6)
                        return new DateTime(int.Parse(tokens[0]), int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]), int.Parse(tokens[4]), int.Parse(tokens[5]));
                    return null;
                }
            }
            catch {
                return null;
            }
        }
    }
}