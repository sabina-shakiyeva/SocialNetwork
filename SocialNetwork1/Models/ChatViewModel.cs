using SocialNetwork1.Entities;

namespace SocialNetwork1.Models
{
    public class ChatViewModel
    {
        public string CurrentUserId { get; set; }
        public string CurrentReceiver { get; set; }
        public Chat CurrentChat { get; set; }
        public IQueryable<Chat> Chats { get; set; }
    }
}