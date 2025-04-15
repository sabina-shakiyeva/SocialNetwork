﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SocialNetwork1.Data;
using SocialNetwork1.Entities;

namespace SocialNetwork1.Hubs
{
    public class ChatHub:Hub
    {
        private readonly UserManager<CustomIdentityUser> _userManager;
        private IHttpContextAccessor _contextAccessor;
        private SocialNetworkDbContext _context;

        public ChatHub(UserManager<CustomIdentityUser> userManager, 
            IHttpContextAccessor contextAccessor,
            SocialNetworkDbContext context)
        {
            _userManager = userManager;
            _contextAccessor = contextAccessor;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext.User);
            var userItem = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            userItem.IsOnline = true;
            await _context.SaveChangesAsync();

            string info = user.UserName + " connected successfully";
            await Clients.Others.SendAsync("Connect", info);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext.User);
            var userItem = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            userItem.IsOnline = false;
            await _context.SaveChangesAsync();

            string info = user.UserName + " disconnect successfully";
            await Clients.Others.SendAsync("Disconnect", info);
        }

        public async Task SendFollow(string id)
        {
            await Clients.User(id).SendAsync("ReceiveNotification");
        }

        public async Task GetMessages(string receiverId,string senderId)
        {
            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext.User);

            await Clients.Users(new String[] { receiverId, senderId }).SendAsync("ReceiveMessages", receiverId, senderId, user.Id);
        }
    }
}
