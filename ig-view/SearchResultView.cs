using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public class SearchResultView
    {
        public int ScrollOffset => _ScrollOffset;

        public SearchResultView(ChatMessage[] results)
        {
            _MessageArray = results.ToArray();
        }

        private ChatMessage[] _MessageArray;
        private int _ScrollOffset;

        public void Present()
        {
            var consoleCols = Console.WindowWidth;
            var consoleRows = Console.WindowHeight;
            Console.BackgroundColor = ConsoleColor.Black;
            for (int i = 0; i < consoleRows; i++)
            {
                Console.SetCursorPosition(0, i);
                if (i + _ScrollOffset < _MessageArray.Length)
                {
                    var message = _MessageArray[i + _ScrollOffset];
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    var linkText = i < InboxView.LinkKeys.Length ? $"[{InboxView.LinkKeys[i]}] " : "    ";
                    var prefixText = $"{linkText} {message.Timestamp} ";
                    Console.Write(prefixText);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(Formatting.PadWhitespace(message.Text ?? string.Empty, consoleCols - prefixText.Length));

                } else
                    Console.Write(new string(' ', consoleCols));
            }
        }

        public void Scroll(int offset)
        {
            _ScrollOffset += offset;
            if (_ScrollOffset < 0)
                _ScrollOffset = 0;
            else if (_ScrollOffset >= _MessageArray.Length)
                _ScrollOffset = _MessageArray.Length - 1;
        }
    }
}
