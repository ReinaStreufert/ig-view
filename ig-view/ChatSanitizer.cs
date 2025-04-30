using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public static class ChatSanitizer
    {
        public static int SanitizeAttachments(this IEnumerable<ChatMessage> messages, string guardTerm, TimeSpan guardDuration)
        {
            var flaggedDateTimes = messages
                .Where(m => m.Text != null && m.Text.Contains(guardTerm, StringComparison.InvariantCultureIgnoreCase))
                .Select(m => m.Timestamp)
                .ToArray();
            var flaggedAttachments = messages
                .Where(m => m.Attachments != null && flaggedDateTimes.Where(t => (t - m.Timestamp).Duration() < guardDuration).Any())
                .SelectMany(m => m.Attachments!)
                .Select(a => a.FilePath);
            var deletedCount = 0;
            foreach (var attachmentPath in flaggedAttachments)
            {
                File.Delete(attachmentPath);
                deletedCount++;
            }
            return deletedCount;
        }
    }
}
