using geeks.Models;

namespace geeks.Commands
{
    public class DeleteEventCommand : Command
    {
        public string EventId { get; set; }
        public override void Execute()
        {
            var model = Session.Load<Event>(EventId);
            if (model != null)
            {
                Session.Delete(model);
            }
        }
    }
}