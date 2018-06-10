using System;
using System.Collections.Generic;
using System.IO;

namespace ImageTools {
    public static class ImageCopyHelper {
        public static string MoveFile(string outputDirectory, string rulesPath, string file) {
            DateTime dateTime = GetCreationDate(file);
            string imagePath = outputDirectory.TrimEnd(Path.DirectorySeparatorChar) + ImagePathBuilder.BuildPath(file, dateTime, rulesPath);
            string directory = Path.GetDirectoryName(imagePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            ConsoleColor oldColor = Console.ForegroundColor;
            if (!File.Exists(imagePath)) {
                File.Copy(file, imagePath, true);
            }
            else {
                if (dateTime != GetCreationDate(imagePath)) {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);
                    string imagePathFormat = Path.Combine(Path.GetDirectoryName(imagePath), name + "_{0}" + extension);
                    int suffix = 1;
                    do {
                        imagePath = String.Format(imagePathFormat, suffix);
                        suffix++;
                    } while (File.Exists(imagePath));
                    File.Copy(file, imagePath, true);
                }
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
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                try {
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
                catch {
                    return null;
                }
            }
        }
    }
}