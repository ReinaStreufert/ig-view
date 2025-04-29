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
                .Select(m => new ChatMessage(this, (JObject)m, Id, _DirPath));
        }
    }

    public class ChatMessage
    {
        public Conversation Conversation { get; }
        public string SenderName { get; }
        public DateTime Timestamp { get; }
        public string? Text { get; }
        public string[]? Photos { get; }

        public ChatMessage(Conversation conversation, JObject json, string conversationId, string dirPath)
        {
            Conversation = conversation;
            SenderName = json["sender_name"]!.ToString();
            Timestamp = DateTime.UnixEpoch.AddMilliseconds((long)json["timestamp_ms"]!);
            var content = json["content"];
            if (content == null)
                Text = null;
            else
                Text = content.Type == JTokenType.String ? content.ToString() : "Not text";
            if (json.ContainsKey("photos"))
            {
                var photosArr = (JArray)json["photos"]!;
                Photos = photosArr
                    .Cast<JObject>()
                    .Select(o =>
                    {
                        var split = o["uri"]!.ToString().Split($"{conversationId}/");
                        if (split.Length == 1)
                            return split[0];
                        return Path.Combine(dirPath, split[1]);
                    })
                    .ToArray();
            }
        }
    }
}
