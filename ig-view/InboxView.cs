using LibChromeDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public class InboxView
    {
        public static readonly string LinkKeys = "0123456789ABCDEGHIKLMNOPQRTUVWXYZ"; // j, f, and s omitted (jump, find and sanitize)

        public InboxView(Inbox inbox)
        {
            _Conversations = inbox.GetConversations().ToArray();
        }

        private Conversation[] _Conversations;
        private int _ScrollOffset;
        private MediaView? _Media;
        private object _MediaLock = new object();

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
                else if (inputKey.Key == ConsoleKey.F)
                {
                    var term = Prompt("Enter search term: ");
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
            Console.Write(Formatting.PadBetween("IG Inbox", "[F] find all [UP] scroll up [DN] scroll down", consoleCols));
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
                    view.Scroll(DateTime.Parse(Prompt("Enter a date and/or time: ")));
                else if (inputKey.Key == ConsoleKey.F)
                {
                    var term = Prompt("Enter search term: ");
                    var results = view.MessageBuffer
                        .Where(m => m.Text != null && m.Text.Contains(term))
                        .ToArray();
                    var jumpedResult = EnterSearchResultLoop(results);
                    if (jumpedResult != null)
                        view.Scroll(jumpedResult.Timestamp);
                } else if (inputKey.Key == ConsoleKey.S)
                {
                    var guardTerm = Prompt("Enter guard term: ");
                    var guardDuration = TimeSpan.FromMinutes(int.Parse(Prompt("Enter guard duration (minutes): ")));
                    var deletedCount = view.MessageBuffer.SanitizeAttachments(guardTerm, guardDuration);
                    Prompt($"Deleted {deletedCount} matching attachment files...Press enter");
                }
                else
                {
                    for (int i = 0; i < LinkKeys.Length; i++)
                    {
                        if (LinkKeys[i] != char.ToUpper(inputKey.KeyChar))
                            continue;
                        var mediaLink = mediaLinks
                            .Where(m => m.Index == i)
                            .FirstOrDefault();
                        if (mediaLink != null)
                            ShowAttachmentMedia(mediaLink.Attachment);
                    }
                }
            }
        }

        private IEnumerable<MediaLink> RenderConversationView(ConversationView view)
        {
            var consoleCols = Console.WindowWidth;
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(Formatting.PadBetween(view.Conversation.Name, "[J] jump [F] find [S] sanitize [UP] scroll up [DN] scroll down [ESC] exit", consoleCols));
            Console.WriteLine(Formatting.PadWhitespace($"participants: {string.Join(", ", view.Conversation.Participants)}", consoleCols));
            return view.Present(2);
        }

        private string Prompt(string msg)
        {
            var consoleCols = Console.WindowWidth;
            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(new string(' ', consoleCols));
            Console.SetCursorPosition(0, 0);
            Console.Write(msg);
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

        private void ShowAttachmentMedia(Attachment attachment)
        {
            MediaView view;
            lock (_MediaLock)
            {
                if (_Media == null)
                {
                    view = new MediaView(attachment, OnMediaClosed);
                    _ = view.LaunchAsync();
                    _Media = view;
                    return;
                }
                else
                    view = _Media;
            }
            _ = view.SetAttachmentAsync(attachment);
        }

        private void OnMediaClosed()
        {
            lock (_MediaLock)
                _Media = null;
        }
    }
}
