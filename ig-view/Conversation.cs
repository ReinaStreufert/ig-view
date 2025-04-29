using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public class Conversation
    {
        public string Name { get; }
        public string Id { get; }
        public string[] Participants { get; }

        public Conversation(string dirPath)
        {
            _DirPath = dirPath;
            var split = Path.GetFileName(_DirPath).Split('_');
            Name = split[0];
            Id = split[split.Length - 1];
            Participants = GetParticipants().ToArray();
        }

        private string _DirPath;

        private IEnumerable<string> GetParticipants()
        {
            var firstPagePath = Path.Combine(_DirPath, "message_1.json");
            var jsonPage = JObject.Parse(Formatting.DecipherInstagramness(File.ReadAllText(firstPagePath)));
            var participants = (JArray)jsonPage["participants"]!;
            return participants
                .Cast<JObject>()
                .Select(p => Formatting.DecipherInstagramness(p["name"]!.ToString()));
        }

        public IEnumerable<ChatMessage> GetMessages()
        {
            return Directory.GetFiles(_DirPath)
                .Select(p => (p, Path.GetFileNameWithoutExtension(p)))
                .Where(p => p.Item2.StartsWith("message_"))
                .OrderBy(p => int.Parse(p.Item2.Split('_')[1]))
                .Select(p => JObject.Parse(Formatting.DecipherInstagramness(File.ReadAllText(p.p))))
                .SelectMany(o => ((JArray)o["messages"]!))
                .Select(m => new ChatMessage(this, (JObject)m, _DirPath));
        }
    }

    public class ChatMessage
    {
        public Conversation Conversation { get; }
        public string SenderName { get; }
        public DateTime Timestamp { get; }
        public string? Text { get; }
        public Attachment[]? Attachments { get; }

        public ChatMessage(Conversation conversation, JObject json, string dirPath)
        {
            Conversation = conversation;
            SenderName = json["sender_name"]!.ToString();
            Timestamp = DateTime.UnixEpoch.AddMilliseconds((long)json["timestamp_ms"]!);
            var content = json["content"];
            if (content == null)
                Text = null;
            else
                Text = content.Type == JTokenType.String ? content.ToString() : "Not text";
            var attachments = Enumerable.Empty<Attachment>();
            if (json.ContainsKey("photos"))
            {
                var photosArr = (JArray)json["photos"]!;
                attachments = attachments.Concat(ParseAttachments(photosArr, dirPath, MessageAttachmentType.Photo));
            }
            if (json.ContainsKey("videos"))
            {
                var videosArr = (JArray)json["videos"]!;
                attachments = attachments.Concat(ParseAttachments(videosArr, dirPath, MessageAttachmentType.Video));
            }
            if (json.ContainsKey("audio_files"))
            {
                var recordingArr = (JArray)json["audio_files"]!;
                attachments = attachments.Concat(ParseAttachments(recordingArr, dirPath, MessageAttachmentType.Audio));
            }
            if (attachments.Any())
                Attachments = attachments.ToArray();
        }

        private IEnumerable<Attachment> ParseAttachments(JArray attachments, string dirPath, MessageAttachmentType type)
        {
            return attachments.Cast<JObject>().Select(o => o["uri"]!.ToString().Split($"{Conversation.Id}/"))
                .Where(split => split.Length > 2)
                .Select(split => new Attachment(type, Path.Combine(dirPath, split[1])));
        }
    }

    public class Attachment
    {
        public MessageAttachmentType Type { get; }
        public string FilePath { get; }

        public Attachment(MessageAttachmentType type, string filePath)
        {
            Type = type;
            FilePath = filePath;
        }
    }

    public enum MessageAttachmentType
    {
        Photo,
        Video,
        Audio
    }
}
