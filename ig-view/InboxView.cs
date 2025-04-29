using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public class InboxView
    {
        public static readonly string _LinkKeys = "0123456789ABCDEFGHIKLMNOPQRTUVWXYZ"; // j and s omitted (jump and search)

        public InboxView(Inbox inbox)
        {
            _Conversations = inbox.GetConversations().ToArray();
        }

        private Conversation[] _Conversations;
        private int _ScrollOffset;

        public void EnterInboxLoop()
        {
            for (; ;)
            {
                RenderInboxMenu();
                var inputKey = Console.ReadKey(true);
                if (inputKey.Key == ConsoleKey.Escape)
                    break;
                else if (inputKey.Key == ConsoleKey.UpArrow)
                    _ScrollOffset--;
                else if (inputKey.Key == ConsoleKey.DownArrow)
                    _ScrollOffset++;
                else
                {
                    for (int i = 0; i < _LinkKeys.Length; i++)
                    {
                        if (_LinkKeys[i] != char.ToUpper(inputKey.KeyChar))
                            continue;
                        var conversationIndex = _ScrollOffset + i;
                        if (conversationIndex < _Conversations.Length)
                            EnterConversationLoop(_Conversations[conversationIndex]);
                    }
                }
                if (_ScrollOffset < 0)
                    _ScrollOffset = 0;
                if (_ScrollOffset >= _Conversations.Length)
                    _ScrollOffset = _Conversations.Length - 1;
            }
        }

        private void RenderInboxMenu()
        {
            var consoleCols = Console.WindowWidth;
            var consoleRows = Console.WindowHeight;
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(0, 0);
            Console.Write(Formatting.PadBetween("IG Inbox", "[S] search all [UP] scroll up [DN] scroll down", consoleCols));
            Console.BackgroundColor = ConsoleColor.Black;
            for (int i = 0; i < consoleRows - 1 && _ScrollOffset + i < _Conversations.Length; i++)
            {
                Console.WriteLine();
                var conversation = _Conversations[i + _ScrollOffset];
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(i < _LinkKeys.Length ? $"[{_LinkKeys[i]}] " : "    ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(Formatting.PadBetween(conversation.Name, conversation.Id, consoleCols - 4));
            }
        }

        private void EnterConversationLoop(Conversation conversation)
        {
            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Formatting.PadWhitespace("Loading conversation...", Console.WindowWidth));
            var view = new ConversationView(conversation);
            for (; ;)
            {
                var mediaLinks = RenderConversationView(view).ToArray();
                var inputKey = Console.ReadKey(true);
                if (inputKey.Key == ConsoleKey.Escape)
                    break;
                else if (inputKey.Key == ConsoleKey.UpArrow)
                    view.Scroll(1);
                else if (inputKey.Key == ConsoleKey.DownArrow)
                    view.Scroll(-1);
            }
        }

        private IEnumerable<MediaLink> RenderConversationView(ConversationView view)
        {
            var consoleCols = Console.WindowWidth;
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(Formatting.PadBetween(view.Conversation.Name, "[J] jump [S] search [UP] scroll up [DN] scroll down [ESC] exit", consoleCols));
            Console.WriteLine(Formatting.PadWhitespace($"participants: {string.Join(", ", view.Conversation.Participants)}", consoleCols));
            return view.Present(2);
        }
    }
}
