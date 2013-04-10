using geeks.Queries;

namespace geeks.Commands
{
    public class DeleteFriendCommand : Command
    {
        public string PersonId { get; set; }

        public override void Execute()
        {
            Query(new PersonByUserIdWithFriends {UserId = CurrentUserId})
                .Friends.RemoveAll(f => f.PersonId == PersonId);
        }
    }
}