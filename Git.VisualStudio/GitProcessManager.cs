﻿namespace Git.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Properties;

    /// <summary>
    /// Manages and starts GIT processes.
    /// </summary>
    public class GitProcessManager : IGitProcessManager
    {
        private readonly string repoDirectory;
        private IOutputLogger outputLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitProcessManager"/> class.
        /// </summary>
        /// <param name="repoDirectory">The location of the GIT repository.</param>
        /// <param name="newOutputLogger">The output logger where to send output.</param>
        public GitProcessManager(string repoDirectory, IOutputLogger newOutputLogger)
        {
            this.repoDirectory = repoDirectory;
            this.outputLogger = newOutputLogger;
        }

        /// <summary>
        /// Runs a new instance of GIT.
        /// </summary>
        /// <param name="gitArguments">The arguments to pass to GIT.</param>
        /// <param name="token">A cancellation token with the ability to cancel the process.</param>
        /// <param name="extraEnvironmentVariables">Any environment variables to pass for the process.</param>
        /// <param name="callerMemberName">The member calling the process.</param>
        /// <param name="includeStandardArguments">If to include standard arguments useful in a script environment.</param>
        /// <returns>A task which will return the response from the GIT process.</returns>
        public async Task<GitCommandResponse> RunGit(string gitArguments, CancellationToken token, IDictionary<string, string> extraEnvironmentVariables = null, [CallerMemberName] string callerMemberName = null, bool includeStandardArguments = true)
        {
            if (includeStandardArguments)
            {
                gitArguments = $"--no-pager -c color.branch=false -c color.diff=false -c color.status=false -c diff.mnemonicprefix=false -c core.quotepath=false {gitArguments}";
            }

            this.outputLogger?.WriteLine($"execute: git {gitArguments}");

            using (Process process = CreateGitProcess(gitArguments, this.repoDirectory))
            {
                if (extraEnvironmentVariables != null)
                {
                    foreach (KeyValuePair<string, string> kvp in extraEnvironmentVariables)
                    {
                        process.StartInfo.EnvironmentVariables.Add(kvp.Key, kvp.Value);
                    }
                }

                StringBuilder output = new StringBuilder();

                process.ErrorDataReceived += (sender, e) => this.OnErrorReceived(e, output);
                process.OutputDataReceived += (sender, e) => this.OnOutputDataReceived(e, output);

                int returnValue = await RunProcessAsync(process, token);

                if (returnValue != 0)
                {
                    return new GitCommandResponse(false, $"{callerMemberName} failed. See output window.", output.ToString(), returnValue);
                }

                return new GitCommandResponse(true, $"{callerMemberName} succeeded.", output.ToString(), returnValue);
            }
        }

        private static Process CreateGitProcess(string arguments, string repoDirectory)
        {
            string gitInstallationPath = GitHelper.GetGitInstallationPath();
            string pathToGit = Path.Combine(Path.Combine(gitInstallationPath, "bin\\git.exe"));
            return new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = pathToGit,
                    Arguments = arguments,
                    WorkingDirectory = repoDirectory,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8
                }, EnableRaisingEvents = true
            };
        }

        private static Task<int> RunProcessAsync(Process process, CancellationToken token)
        {
            bool started = process.Start();
            if (!started)
            {
                // you may allow for the process to be re-used (started = false) 
                // but I'm not sure about the guarantees of the Exited event in such a case
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return Task.Run(
                () =>
                    {
                        process.WaitForExit();
                        return process.ExitCode;
                    }, 
                token);
        }

        private void OnOutputDataReceived(DataReceivedEventArgs e, StringBuilder sb)
        {
            if (e.Data == null)
            {
                return;
            }

            sb.AppendLine(e.Data);
            this.outputLogger?.WriteLine(e.Data);
        }

        private void OnErrorReceived(DataReceivedEventArgs e, StringBuilder sb)
        {
            if (e.Data == null)
            {
                return;
            }

            if (!e.Data.StartsWith("fatal:", StringComparison.OrdinalIgnoreCase))
            {
                this.outputLogger?.WriteLine(e.Data);
                sb.AppendLine(e.Data);
                return;
            }

            this.outputLogger?.WriteLine(string.Format(Resources.ErrorRunningGit, e.Data));
        }
    }
}
