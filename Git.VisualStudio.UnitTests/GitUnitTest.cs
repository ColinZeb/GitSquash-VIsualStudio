﻿namespace Git.VisualStudio.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class GitUnitTest
    {
        [Fact]
        public async void TestGitHistoryBranchOnly()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDirectory);

            GitProcessManager local = new GitProcessManager(tempDirectory, null);

            var result = await local.RunGit("init", CancellationToken.None);

            result.Success.Should().Be(true, "Must be able to init");

            int numberCommits = 10;
            for (int i = 0; i < numberCommits; ++i)
            {
                File.WriteAllText(Path.Combine(tempDirectory, Path.GetRandomFileName()), "Hello World" + i);
                result = await local.RunGit("add -A", CancellationToken.None);
                result.Success.Should().Be(true, "Must be able to add");
                result = await local.RunGit($"commit -m \"{i}\"", CancellationToken.None);
                result.Success.Should().Be(true, "Must be able to commit");
            }

            BranchManager branchManager = new BranchManager(tempDirectory, null);

            var commits = await branchManager.GetCommitsForBranch(new GitBranch("master", false), 0, 0, GitLogOptions.BranchOnlyAndParent, CancellationToken.None);

            commits.Count.Should().Be(numberCommits, $"We have done {numberCommits} commits");

            commits.Should().BeInDescendingOrder(x => x.DateTime);

            result = await local.RunGit("branch test1", CancellationToken.None);
            result.Success.Should().Be(true, "Must be able create branch");

            result = await local.RunGit("checkout test1", CancellationToken.None);
            result.Success.Should().Be(true, "Must be able checkout branch");

            for (int i = 0; i < numberCommits; ++i)
            {
                File.WriteAllText(Path.Combine(tempDirectory, Path.GetRandomFileName()), "Hello World" + i);
                result = await local.RunGit("add -A", CancellationToken.None);
                result.Success.Should().Be(true, "Must be able to add");
                result = await local.RunGit($"commit -m \"{i}\"", CancellationToken.None);
                result.Success.Should().Be(true, "Must be able to commit");
            }

            commits = await branchManager.GetCommitsForBranch(new GitBranch("test1", false), 0, 0, GitLogOptions.BranchOnlyAndParent, CancellationToken.None);

            commits.Count.Should().Be(numberCommits + 1, $"We have done {numberCommits + 1} commits");
        }
    }
}
