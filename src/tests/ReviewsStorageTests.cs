using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

namespace MultilinerBot.Tests
{
    [TestFixture]
    public class ReviewsStorageTests
    {
        [Test]
        public void TestAddAndModifyReviewSameRepo()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                List<Review> storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(0, storedReviews.Count);

                Review rev1 = new Review("rep", "11", "21", "pending", "title_review_11");
                Review rev2 = new Review("rep", "13", "21", "pending", "title_review_13");

                ReviewsStorage.WriteReview(rev1, filePath);
                ReviewsStorage.WriteReview(rev2, filePath);

                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(2, storedReviews.Count);
                CheckReviewExists(storedReviews, rev1);
                CheckReviewExists(storedReviews, rev2);

                Review rev2Modified = new Review("rep", "13", "21", "approved", "title_review_13_modified");

                ReviewsStorage.WriteReview(rev2Modified, filePath);

                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);

                Assert.AreEqual(2, storedReviews.Count);
                CheckReviewExists(storedReviews, rev1);
                CheckReviewExists(storedReviews, rev2Modified);
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test]
        public void TestAddReviewSameIdDifferentRepo()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                List<Review> storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(0, storedReviews.Count);

                Review rev1 = new Review("rep", "11", "21", "pending", "title_review_11");
                Review rev2 = new Review("rep", "13", "21", "pending", "title_review_13");

                ReviewsStorage.WriteReview(rev1, filePath);
                ReviewsStorage.WriteReview(rev2, filePath);

                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(2, storedReviews.Count);
                CheckReviewExists(storedReviews, rev1);
                CheckReviewExists(storedReviews, rev2);

                Review revOtherRepoSameID = new Review("other", "13", "21", "approved", "title_review_13_other_rep");

                ReviewsStorage.WriteReview(revOtherRepoSameID, filePath);

                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);

                Assert.AreEqual(3, storedReviews.Count);
                CheckReviewExists(storedReviews, rev1);
                CheckReviewExists(storedReviews, rev2);
                CheckReviewExists(storedReviews, revOtherRepoSameID);
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test]
        public void TestDeleteReview()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                List<Review> storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(0, storedReviews.Count);

                Review iDontExist = new Review("rep", "1", "2", "discarded", "no title");
                ReviewsStorage.DeleteReview(iDontExist, filePath);
                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(0, storedReviews.Count);

                Review rev1 = new Review("rep", "11", "21", "pending", "title_review_11");
                Review rev2 = new Review("rep", "13", "21", "pending", "title_review_13");
                Review revOtherRepoSameID = new Review("other", "13", "21", "approved", "title_review_13_other_rep");

                ReviewsStorage.WriteReview(rev1, filePath);
                ReviewsStorage.WriteReview(rev2, filePath);
                ReviewsStorage.WriteReview(revOtherRepoSameID, filePath);

                ReviewsStorage.DeleteReview(iDontExist, filePath);

                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(3, storedReviews.Count);
                CheckReviewExists(storedReviews, rev1);
                CheckReviewExists(storedReviews, rev2);
                CheckReviewExists(storedReviews, revOtherRepoSameID);

                ReviewsStorage.DeleteReview(rev2, filePath);
                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(2, storedReviews.Count);
                CheckReviewExists(storedReviews, rev1);
                CheckReviewExists(storedReviews, revOtherRepoSameID);

                ReviewsStorage.DeleteReview(revOtherRepoSameID, filePath);
                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(1, storedReviews.Count);
                CheckReviewExists(storedReviews, rev1);

                ReviewsStorage.DeleteReview(rev1, filePath);
                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(0, storedReviews.Count);

                ReviewsStorage.DeleteReview(rev1, filePath);
                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(0, storedReviews.Count);
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test]
        public void TestDeleteBranchReviews()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                List<Review> storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(0, storedReviews.Count);

                Review rev1 = new Review("rep", "11", "21", "pending", "title_review_11");
                Review rev2 = new Review("rep", "13", "21", "pending", "title_review_13");
                Review rev3 = new Review("rep", "15", "22", "pending", "title_review_15");
                Review revOtherRepoSameID = new Review("other", "13", "21", "approved", "title_review_13_other_rep");

                ReviewsStorage.WriteReview(rev1, filePath);
                ReviewsStorage.WriteReview(rev2, filePath);
                ReviewsStorage.WriteReview(rev3, filePath);
                ReviewsStorage.WriteReview(revOtherRepoSameID, filePath);

                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(4, storedReviews.Count);

                ReviewsStorage.DeleteBranchReviews("rep", "21", filePath);
                storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(2, storedReviews.Count);
                CheckReviewExists(storedReviews, rev3);
                CheckReviewExists(storedReviews, revOtherRepoSameID);
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test]
        public void TestGetBranchReviews()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                Review rev1 = new Review("rep", "11", "21", "pending", "title_review_11");
                Review rev2 = new Review("rep", "13", "22", "pending", "title_review_13");
                Review rev3 = new Review("rep", "15", "21", "pending", "title_review_15");
                Review revOtherRepoSameID = new Review("other", "13", "21", "approved", "title_review_13_other_rep");

                ReviewsStorage.WriteReview(rev1, filePath);
                ReviewsStorage.WriteReview(rev2, filePath);
                ReviewsStorage.WriteReview(rev3, filePath);
                ReviewsStorage.WriteReview(revOtherRepoSameID, filePath);

                List<Review> storedReviews = ReviewsStorage.Testing.TestingLoadReviews(filePath);
                Assert.AreEqual(4, storedReviews.Count);

                List<Review> branchReviews = ReviewsStorage.GetBranchReviews("rep", "21", filePath);

                Assert.AreEqual(2, branchReviews.Count);
                CheckReviewExists(branchReviews, rev1);
                CheckReviewExists(branchReviews, rev3);

                branchReviews = ReviewsStorage.GetBranchReviews("other", "21", filePath);
                Assert.AreEqual(1, branchReviews.Count);
                CheckReviewExists(branchReviews, revOtherRepoSameID);

                branchReviews = ReviewsStorage.GetBranchReviews("rep", "22", filePath);
                Assert.AreEqual(1, branchReviews.Count);
                CheckReviewExists(branchReviews, rev2);

                branchReviews = ReviewsStorage.GetBranchReviews("rep", "9999999999", filePath);
                Assert.AreEqual(0, branchReviews.Count);

                branchReviews = ReviewsStorage.GetBranchReviews("iDontExist", "21", filePath);
                Assert.AreEqual(0, branchReviews.Count);
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        void CheckReviewExists(List<Review> storedReviews, Review toCheck)
        {
            foreach(Review stored in storedReviews)
            {
                if (stored.Repository == toCheck.Repository &&
                    stored.ReviewId == toCheck.ReviewId &&
                    stored.BranchId == toCheck.BranchId &&
                    stored.ReviewStatus == toCheck.ReviewStatus &&
                    stored.ReviewTitle == toCheck.ReviewTitle)
                {
                    return;
                }
            }

            Assert.Fail("Review on rep {0} with ID {1} and title {2} and status {3} not found!",
                toCheck.Repository, toCheck.ReviewId, toCheck.ReviewTitle, toCheck.ReviewStatus);
        }
    }
}
