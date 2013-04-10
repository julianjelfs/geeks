using geeks.Queries;

namespace geeks.Commands
{
    public class DeleteAllFriendsCommand : Command
    {
        public override void Execute()
        {
            Query(new PersonByUserIdWithFriends { UserId = CurrentUserId })
                .Friends.Clear();
        }
    }
}