using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public class Inbox
    {
        public Inbox(string dirPath)
        {
            _DirPath = dirPath;
        }

        private string _DirPath;

        public IEnumerable<Conversation> GetConversations()
        {
            return Directory.GetDirectories(_DirPath)
                .Select(p => new Conversation(p));
        }
    }
}
