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
        static string[] extensions = new string[] { ".sh", ".ps1", ".cmd", ".exe", ".bat", "" };

        PathBuilder? executable;
        string arguments = string.Empty;

        public Executable(PathBuilder name)
        {
            executable = PathBuilder.From(name);
            if (!executable.IsFile)
            {
                var pathEnvironment = Environment.GetEnvironmentVariable("PATH") ?? String.Empty;
                var windows = OperatingSystem.IsWindows();
                var pathEntries = pathEnvironment.Split(windows ? ';' : ':');
                var execNames = extensions.Select(ext => $"{name}{ext}");

                foreach (var pathEntry in pathEntries)
                {
                    foreach (var exeName in execNames)
                    {
                        var execPath = PathBuilder.From(pathEntry) / exeName;
                        if (execPath.IsFile)
                        {
                            executable = execPath;
                            break;
                        }    
                    }
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
            if (executable.Extension == ".ps1")
            {
                var pwsh = new Executable("pwsh"!);
                if (!pwsh.Exists) {
                    throw new Exception("PowerShell (pwsh) is not available in the path.");
                }

                pInfo.FileName = pwsh.Path!;
                pInfo.Arguments = $"-command {this.Path} ";
            }
            else {
                pInfo.FileName = executable;
            }


            pInfo.WorkingDirectory = workingDirectory ?? PathBuilder.CurrentDirectory;
            if (!String.IsNullOrWhiteSpace(arguments))
                pInfo.Arguments += arguments;
            Process? p = System.Diagnostics.Process.Start(pInfo);
            if (p != null)
                await p.WaitForExitAsync();

        }
    }
}
