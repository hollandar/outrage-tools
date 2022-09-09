using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compose.Path
{
    public class Executable
    {
        static string[] extensions = new string[] { "", ".sh", ".ps1", ".cmd", ".exe", ".bat" };

        PathBuilder? executable;
        public Executable(PathBuilder name)
        {
            executable = PathBuilder.From(name);
            if (!executable.IsFile)
            {
                var pathEnvironment = Environment.GetEnvironmentVariable("PATH") ?? String.Empty;
                var windows = OperatingSystem.IsWindows();
                var pathEntries = pathEnvironment.Split(windows ? ';' : ':');

                foreach (var pathEntry in pathEntries)
                {
                    var execNames = extensions.Select(ext => $"{name}{ext}");
                    executable = execNames.Select(execName => PathBuilder.From(pathEntry) / execName).Where(path => path.IsFile).FirstOrDefault();
                    if (executable != null) break;
                }

            }
        }

        public bool Exists => executable != null;
        public PathBuilder? Path => executable;

        public async Task ExecuteAsync(string? arguments = null, PathBuilder? workingDirectory = null)
        {
            ArgumentNullException.ThrowIfNull(executable, "An executable was not found.");

            var pInfo = new ProcessStartInfo();
            pInfo.UseShellExecute = false;
            pInfo.FileName = executable;

            pInfo.WorkingDirectory = workingDirectory ?? PathBuilder.CurrentDirectory;
            if (!String.IsNullOrWhiteSpace(arguments))
                pInfo.Arguments = arguments;
            Process? p = System.Diagnostics.Process.Start(pInfo);
            if (p != null)
                await p.WaitForExitAsync();

        }
    }
}
