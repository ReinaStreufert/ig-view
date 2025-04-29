using System.Text;

namespace ig_view
{
    public class ConversationView
    {
        public Conversation Conversation { get; }
        public ChatMessage[] MessageBuffer => _MessageArray;

        public ConversationView(Conversation conversation)
        {
            Conversation = conversation;
            _MessageArray = conversation.GetMessages().ToArray();
        }

        private ChatMessage[] _MessageArray; // where first index is the newest message, last is the oldest.
        private int _NewestVisibleMessage = 0;
        private Dictionary<string, ConsoleColor> _ParticipantColors = new Dictionary<string, ConsoleColor>();
        private int _ColoredParticipants = 0;
        private readonly ConsoleColor[] _ParticipantColorKeys = new[] { ConsoleColor.Magenta, ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.DarkMagenta, ConsoleColor.DarkCyan, ConsoleColor.DarkYellow, ConsoleColor.DarkGreen, ConsoleColor.DarkBlue };

        public IEnumerable<MediaLink> Present(int headerRows)
        {
            var consoleRows = Console.WindowHeight;
            var consoleCols = Console.WindowWidth;
            var currentRow = consoleRows;
            var linkCount = 0;
            for (int currentMessageIndex = _NewestVisibleMessage; currentRow >= headerRows && currentMessageIndex < _MessageArray.Length; currentMessageIndex++)
            {
                var message = _MessageArray[currentMessageIndex];
                var messageBody = message.Text == null ? null : WrapMessageBody(message.Text, consoleCols);
                var messageRows = (messageBody?.Length ?? 0) + (message.Photos == null ? 1 : 2);
                currentRow -= messageRows;
                Console.SetCursorPosition(0, Math.Max(currentRow, headerRows));
                var participantColor = GetParticipantColor(message.SenderName);
                var messageLine = 0;
                var linesWritten = false;
                if (currentRow >= headerRows)
                {
                    Console.BackgroundColor = participantColor;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write(Formatting.PadBetween(message.SenderName, message.Timestamp.ToString(), consoleCols));
                    linesWritten = true;
                }
                messageLine++;
                Console.BackgroundColor = ConsoleColor.Black;
                if (message.Attachments != null)
                {
                    if (currentRow + messageLine >= headerRows)
                    {
                        if (linesWritten)
                            Console.WriteLine();
                        linesWritten = true;
                        var attachmentsLine = new StringBuilder();
                        attachmentsLine.Append("Attachments: ");
                        var first = true;
                        foreach (var attachment in message.Attachments)
                        {
                            if (first)
                                first = false;
                            else
                                attachmentsLine.Append(" / ");
                            var index = linkCount++;
                            yield return new MediaLink(index, attachment);
                            var verb = attachment.Type switch
                            {
                                MessageAttachmentType.Photo => "view",
                                MessageAttachmentType.Video => "watch",
                                MessageAttachmentType.Audio => "listen",
                                _ => throw new NotImplementedException()
                            }; // grammar is important
                            attachmentsLine.Append($"Press [{InboxView.LinkKeys[index]}] to {verb}");
                        }
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(Formatting.PadWhitespace(attachmentsLine.ToString(), consoleCols));
                    }
                    messageLine++;
                }
                Console.ForegroundColor = participantColor;
                if (messageBody != null)
                {
                    for (int i = 0; i < messageBody.Length; i++)
                    {
                        if (currentRow + messageLine + i >= headerRows)
                        {
                            if (linesWritten)
                                Console.WriteLine();
                            linesWritten = true;
                            Console.Write(Formatting.PadWhitespace(messageBody[i], consoleCols));
                        }
                    }
                }
            }
            if (currentRow > headerRows)
            {
                for (int i = headerRows; i < currentRow; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', consoleCols));
                }
            }
        }

        public void Scroll(int offset)
        {
            _NewestVisibleMessage += offset;
            if (_NewestVisibleMessage < 0)
                _NewestVisibleMessage = 0;
            else if (_NewestVisibleMessage >= _MessageArray.Length)
                _NewestVisibleMessage = _MessageArray.Length - 1;
        }

        public void Scroll(DateTime threshold)
        {
            _NewestVisibleMessage = _MessageArray
                .Select((m, i) => (m, i))
                .Where(t => t.m.Timestamp > threshold)
                .Last().i;
        }

        private ConsoleColor GetParticipantColor(string participant)
        {
            if (_ParticipantColors.TryGetValue(participant, out var existingAssignment))
                return existingAssignment;
            var assignedColor = _ParticipantColorKeys[_ColoredParticipants++];
            _ParticipantColors.Add(participant, assignedColor);
            return assignedColor;
        }

        private string[] WrapMessageBody(string text, int colCount)
        {
            var words = text.ReplaceLineEndings().Split(' ');
            var result = new List<string>();
            var currentLine = new StringBuilder();
            foreach (var word in words)
            {
                if (currentLine.Length + word.Length < colCount)
                {
                    currentLine.Append(word.Replace(Environment.NewLine, null));
                }
                else if (word.Length >= colCount)
                {
                    // break between characters for giant contiguous letters
                    int wordPos = 0;
                    while (wordPos < word.Length)
                    {
                        var segmentLen = Math.Min(colCount - currentLine.Length, word.Length - wordPos);
                        currentLine.Append(word.Substring(wordPos, segmentLen));
                        wordPos += segmentLen;
                        result.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                    continue;
                }
                else
                {
                    // break between words (word-wrap)
                    result.Add(currentLine.ToString());
                    currentLine = new StringBuilder();
                    currentLine.Append(word.Replace(Environment.NewLine, null));
                }
                if (currentLine.Length + 1 < colCount)
                    currentLine.Append(" ");
                if (word.Contains(Environment.NewLine))
                {
                    result.Add(currentLine.ToString());
                    currentLine.Clear();
                }
            }
            if (currentLine.Length > 0)
                result.Add(currentLine.ToString());
            return result.ToArray();
        }


    }

    public class MediaLink
    {
        public int Index { get; }
        public Attachment Attachment { get; }

        public MediaLink(int index, Attachment attachment)
        {
            Index = index;
            Attachment = attachment;
        }
    }
}
