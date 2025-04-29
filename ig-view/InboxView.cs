using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public class InboxView
    {
        public static readonly string LinkKeys = "0123456789ABCDEFGHIKLMNOPQRTUVWXYZ"; // j and s omitted (jump and search)

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
                else if (inputKey.Key == ConsoleKey.S)
                {
                    var term = PromptSearchTerm();
                    var results = _Conversations
                        .SelectMany(c => c.GetMessages())
                        .Where(m => m.Text != null && m.Text.Contains(term))
                        .ToArray();
                    var jumpedResult = EnterSearchResultLoop(results);
                    if (jumpedResult != null)
                        EnterConversationLoop(jumpedResult.Conversation, jumpedResult.Timestamp);
                }
                else
                {
                    for (int i = 0; i < LinkKeys.Length; i++)
                    {
                        if (LinkKeys[i] != char.ToUpper(inputKey.KeyChar))
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
                Console.Write(i < LinkKeys.Length ? $"[{LinkKeys[i]}] " : "    ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(Formatting.PadBetween(conversation.Name, conversation.Id, consoleCols - 4));
            }
        }

        private void EnterConversationLoop(Conversation conversation, DateTime jump = default)
        {
            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Formatting.PadWhitespace("Loading conversation...", Console.WindowWidth));
            var view = new ConversationView(conversation);
            if (jump != default)
                view.Scroll(jump);
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
                else if (inputKey.Key == ConsoleKey.J)
                    view.Scroll(PromptJumpDate());
                else if (inputKey.Key == ConsoleKey.S)
                {
                    var term = PromptSearchTerm();
                    var results = view.MessageBuffer
                        .Where(m => m.Text != null && m.Text.Contains(term))
                        .ToArray();
                    var jumpedResult = EnterSearchResultLoop(results);
                    if (jumpedResult != null)
                        view.Scroll(jumpedResult.Timestamp);
                }
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

        private DateTime PromptJumpDate()
        {
            var consoleCols = Console.WindowWidth;
            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(new string(' ', consoleCols));
            Console.SetCursorPosition(0, 0);
            Console.CursorVisible = true;
            Console.Write("Enter a date and/or time: ");
            var result = DateTime.Parse(Console.ReadLine()!);
            Console.CursorVisible = false;
            return result;
        }

        private string PromptSearchTerm()
        {
            var consoleCols = Console.WindowWidth;
            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(new string(' ', consoleCols));
            Console.SetCursorPosition(0, 0);
            Console.Write("Enter search term: ");
            Console.CursorVisible = true;
            var result = Console.ReadLine()!;
            Console.CursorVisible = false;
            return result;
        }

        private ChatMessage? EnterSearchResultLoop(ChatMessage[] results)
        {
            var view = new SearchResultView(results);
            for (; ;)
            {
                view.Present();
                var inputKey = Console.ReadKey(true);
                if (inputKey.Key == ConsoleKey.Escape)
                    return null;
                else if (inputKey.Key == ConsoleKey.UpArrow)
                    view.Scroll(-1);
                else if (inputKey.Key == ConsoleKey.DownArrow)
                    view.Scroll(1);
                else
                {
                    for (int i = 0; i < LinkKeys.Length; i++)
                    {
                        if (LinkKeys[i] != char.ToUpper(inputKey.KeyChar))
                            continue;
                        var resultIndex = view.ScrollOffset + i;
                        if (resultIndex < results.Length)
                            return results[resultIndex];
                    }
                }
            }
        }
    }
}
