using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class PushFixture : BaseFixture
    {
        private void OnPushStatusError(PushStatusError pushStatusErrors)
        {
            Assert.True(false, string.Format("Failed to update reference '{0}': {1}",
                pushStatusErrors.Reference, pushStatusErrors.Message));
        }

        private void AssertPush(Action<Repository> push)
        {
            var scd = BuildSelfCleaningDirectory();
            using (var originalRepo = new Repository(CloneBareTestRepo()))
            using (Repository clonedRepo = Repository.Clone(originalRepo.Info.Path, scd.RootedDirectoryPath))
            {
                Remote remote = clonedRepo.Network.Remotes["origin"];

                // Compare before
                Assert.Equal(originalRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier,
                             clonedRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier);
                Assert.Equal(
                    clonedRepo.Network.ListReferences(remote).Single(r => r.CanonicalName == "refs/heads/master"),
                    clonedRepo.Refs.Head.ResolveToDirectReference());

                // Change local state (commit)
                const string relativeFilepath = "new_file.txt";
                string filePath = Path.Combine(clonedRepo.Info.WorkingDirectory, relativeFilepath);
                File.WriteAllText(filePath, "__content__");
                clonedRepo.Index.Stage(relativeFilepath);
                clonedRepo.Commit("__commit_message__", DummySignature, DummySignature);

                // Assert local state has changed
                Assert.NotEqual(originalRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier,
                                clonedRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier);
                Assert.NotEqual(
                    clonedRepo.Network.ListReferences(remote).Single(r => r.CanonicalName == "refs/heads/master"),
                    clonedRepo.Refs.Head.ResolveToDirectReference());

                // Push the change upstream (remote state is supposed to change)
                push(clonedRepo);

                // Assert that both local and remote repos are in sync
                Assert.Equal(originalRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier,
                             clonedRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier);
                Assert.Equal(
                    clonedRepo.Network.ListReferences(remote).Single(r => r.CanonicalName == "refs/heads/master"),
                    clonedRepo.Refs.Head.ResolveToDirectReference());
            }
        }

        [Fact]
        public void CanPushABranchTrackingAnUpstreamBranch()
        {
            AssertPush(repo => repo.Network.Push(repo.Head));
            AssertPush(repo => repo.Network.Push(repo.Branches["master"]));
            AssertPush(repo => repo.Network.Push(repo.Network.Remotes["origin"], "HEAD", @"refs/heads/master", OnPushStatusError));
        }

        [Fact]
        public void PushingABranchThatDoesNotTrackAnUpstreamBranchThrows()
        {
            Assert.Throws<LibGit2SharpException>(
                () =>
                AssertPush(repo =>
                    {
                        Branch branch = repo.Branches["master"];
                        repo.Branches.Update(branch, b => b.TrackedBranch = null);
                        repo.Network.Push(branch);
                    }));
        }
    }
}
