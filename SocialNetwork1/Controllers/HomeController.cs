using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetwork1.Data;
using SocialNetwork1.Entities;
using SocialNetwork1.Models;
using System.Diagnostics;

namespace SocialNetwork1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<CustomIdentityUser> _userManager;
        private readonly SocialNetworkDbContext _context;

        public HomeController(ILogger<HomeController> logger,
            UserManager<CustomIdentityUser> userManager,
            SocialNetworkDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            ViewBag.User = user;
            return View();
        }

        public async Task<ActionResult> GetAllUsers()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var myRequests = _context.FriendRequests.Where(r => r.SenderId == user.Id);

            var myfriends = _context.Friends.Where(f => f.OwnId == user.Id || f.YourFriendId == user.Id);


            var users = await _context.Users
                .Where(u => u.Id != user.Id)
                .Select(u => new CustomIdentityUser
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    IsFriend = (myfriends.FirstOrDefault(f => f.OwnId == u.Id || f.YourFriendId == u.Id) != null),
                    IsOnline = u.IsOnline,
                    Image = u.Image,
                    Email = u.Email,
                    HasRequestPending = (myRequests.FirstOrDefault(r => r.ReceiverId == u.Id && r.Status == "Request") != null)
                })
                .ToListAsync();

            return Ok(users);
        }

        public async Task<ActionResult> SendFollow(string id)
        {
            var sender = await _userManager.GetUserAsync(HttpContext.User);
            var receiverUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (receiverUser != null)
            {
                await _context.FriendRequests.AddAsync(new FriendRequest
                {
                    Content = $"{sender.UserName} sent friend request at {DateTime.Now.ToLongDateString()}",
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = id,
                    Status = "Request"
                });

                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest();
        }


        [HttpDelete]
        public async Task<ActionResult> TakeRequest(string id)
        {
            var current = await _userManager.GetUserAsync(HttpContext.User);
            var request = await _context.FriendRequests.FirstOrDefaultAsync(r => r.SenderId == current.Id && r.ReceiverId == id);
            if (request == null) return NotFound();
            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> DeclineRequest(int id, string senderId)
        {
            try
            {
                var current = await _userManager.GetUserAsync(HttpContext.User);
                var request = await _context.FriendRequests.FirstOrDefaultAsync(r => r.Id == id);
                _context.FriendRequests.Remove(request);

                _context.FriendRequests.Add(new FriendRequest
                {
                    Content = $"{current.UserName} declined your friend request at {DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}",
                    SenderId = current.Id,
                    Sender = current,
                    ReceiverId = senderId,
                    Status = "Notification"
                });

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<ActionResult> AcceptRequest(string senderId, string receiverId, int requestId)
        {
            var senderUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == senderId);
            var receiverUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == receiverId);

            if (senderUser == null || receiverUser == null) return BadRequest();
            _context.FriendRequests.Add(new FriendRequest
            {
                SenderId = receiverId,
                ReceiverId = senderId,
                Sender = receiverUser,
                Status = "Notification",
                Content = $"{receiverUser.UserName} accepted friend request at ${DateTime.Now}"
            });

            var request = await _context.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            _context.FriendRequests.Remove(request);

            _context.Friends.Add(new Friend
            {
                OwnId = senderId,
                YourFriendId = receiverId
            });


            await _context.SaveChangesAsync();
            return Ok();
        }

        public async Task<ActionResult> DeleteRequest(int id)
        {
            try
            {
                var request = await _context.FriendRequests.FirstOrDefaultAsync(r => r.Id == id);
                if (request == null) return NotFound();
                _context.FriendRequests.Remove(request);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<ActionResult> GetAllRequests()
        {
            var current = await _userManager.GetUserAsync(HttpContext.User);
            var requests = _context.FriendRequests.Where(r => r.ReceiverId == current.Id);
            return Ok(requests);
        }

        public async Task<IActionResult> GoChat(string id)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var chat = await _context.Chats.Include(nameof(Chat.Messages)).FirstOrDefaultAsync(c => c.SenderId == user.Id && c.ReceiverId == id
            || c.SenderId == id && c.ReceiverId == user.Id
            );

            if (chat == null)
            {
                chat = new Chat
                {
                    ReceiverId = id,
                    SenderId = user.Id,
                    Messages = new List<Message>()
                };

                await _context.Chats.AddAsync(chat);
                await _context.SaveChangesAsync();
            }

            var chats = _context.Chats.Include(nameof(Chat.Messages)).Where(c => c.SenderId == user.Id || c.ReceiverId == user.Id);

            var chatBlocks = from c in chats
                             let receiver = (user.Id != c.ReceiverId) ? c.Receiver : _context.Users.FirstOrDefault(u => u.Id == c.SenderId)
                             select new Chat
                             {
                                 Messages = c.Messages,
                                 Id = c.Id,
                                 SenderId = c.SenderId,
                                 Receiver = receiver,
                                 ReceiverId = receiver.Id
                             };

            var model = new ChatViewModel
            {
                CurrentUserId = user.Id,
                CurrentReceiver = id,
                CurrentChat = chat,
                Chats = chatBlocks
            };

            return View(model);

        }


        [HttpPost(Name ="AddMessage")]
        public async Task<ActionResult> AddMessage(MessageModel model)
        {
            var chat=await _context.Chats.FirstOrDefaultAsync(c=>c.SenderId==model.SenderId && c.ReceiverId==model.ReceiverId
            || c.SenderId == model.ReceiverId && c.ReceiverId == model.SenderId
            );

            if (chat != null)
            {
                var message = new Message
                {
                    ChatId = chat.Id,
                    Content = model.Content,
                    DateTime = DateTime.Now,
                    IsImage = false,
                    HasSeen = false,
                    SenderId = model.SenderId
                };

                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest("No chat exist");
        }

        public async Task<ActionResult> GetAllMessages(string receiverId,string senderId)
        {
            var chat = await _context.Chats.Include(nameof(Chat.Messages)).FirstOrDefaultAsync(c => c.SenderId == senderId && c.ReceiverId == receiverId
            || c.ReceiverId == senderId && c.SenderId == receiverId);
            if (chat != null)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                return Ok(new { Messages = chat.Messages, CurrentUserId = user.Id });
            }
            return Ok();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
