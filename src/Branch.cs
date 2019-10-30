namespace MultilinerBot
{
    public class Branch
    {
        internal readonly string Repository;
        internal readonly string Id;
        public string FullName;
        internal readonly string Owner;
        internal readonly string Comment;

        public Branch(
            string repository,
            string id,
            string fullName,
            string owner,
            string comment)
        {
            Repository = repository;
            Id = id;
            FullName = fullName;
            Owner = owner;
            Comment = comment;
        }
    }

    internal class BranchWithReview
    {
        internal Branch Branch;
        internal Review Review;
    }
}
