using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace EtaxSignUtil
{
    internal static class JavaRuntimeLocator
    {
        public static List<string> GetExecutablePaths()
        {
            var paths = new List<string>();
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string directory in path.Split(';'))
                AddExecutable(paths, directory, "java.exe");

            AddExecutable(paths, Environment.GetEnvironmentVariable("JAVA_HOME"), @"bin\java.exe");

            // Keep the legacy installation folders as a fallback after PATH/JAVA_HOME.
            foreach (string root in new[] { @"C:\Program Files (x86)\Java", @"C:\Program Files\Java" })
            {
                try
                {
                    if (Directory.Exists(root))
                        foreach (string directory in Directory.GetDirectories(root))
                            AddExecutable(paths, directory, @"bin\java.exe");
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (SecurityException) { }
            }
            return paths;
        }

        private static void AddExecutable(List<string> paths, string directory, string executable)
        {
            if (String.IsNullOrEmpty(directory))
                return;
            directory = Environment.ExpandEnvironmentVariables(directory.Trim().Trim('"'));
            if (directory.Length == 0)
                return;
            try
            {
                string candidate = Path.GetFullPath(Path.Combine(directory, executable));
                if (!File.Exists(candidate))
                    return;
                foreach (string existing in paths)
                    if (String.Equals(existing, candidate, StringComparison.OrdinalIgnoreCase))
                        return;
                paths.Add(candidate);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (SecurityException) { }
        }
    }
}
