using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CurrentOperationFixture : BaseFixture
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CurrentOperationIsNoneForNewRepo(bool isBare)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.DirectoryPath, isBare))
            {
                Assert.Equal(CurrentOperation.None, repo.Info.CurrentOperation);
            }
        }

        [Fact]
        public void CurrentOperationInNoneForABareRepo()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(CurrentOperation.None, repo.Info.CurrentOperation);
            }
        }

        [Theory]
        [InlineData("MERGE_HEAD", CurrentOperation.Merge)]
        [InlineData("REVERT_HEAD", CurrentOperation.Revert)]
        [InlineData("CHERRY_PICK_HEAD", CurrentOperation.CherryPick)]
        [InlineData("BISECT_LOG", CurrentOperation.Bisect)]
        [InlineData("rebase-apply/rebasing", CurrentOperation.Rebase)]
        [InlineData("rebase-apply/applying", CurrentOperation.ApplyMailbox)]
        [InlineData("rebase-apply/whatever", CurrentOperation.ApplyMailboxOrRebase)]
        [InlineData("rebase-merge/interactive", CurrentOperation.RebaseInteractive)]
        [InlineData("rebase-merge/whatever", CurrentOperation.RebaseMerge)]
        public void CurrentOperationHasExpectedPendingOperationValues(string stateFile, CurrentOperation expectedState)
        {
            string path = CloneStandardTestRepo();

            Touch(Path.Combine(path, ".git"), stateFile);

            using (var repo = new Repository(path))
            {
                Assert.Equal(expectedState, repo.Info.CurrentOperation);
            }
        }
    }
}
