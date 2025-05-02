using System.Linq;

namespace ig_view
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var inbox = new Inbox("C:\\Users\\Reina\\Documents\\instagram-reinastreufert-2025-04-30-yhQkIkuu\\your_instagram_activity\\messages\\inbox");//new Inbox(Path.Combine(Environment.CurrentDirectory, "messages\\inbox"));
            var inboxView = new InboxView(inbox);
            Console.CursorVisible = false;
            inboxView.EnterInboxLoop();
        }
    }
}