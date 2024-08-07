using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Data;
using System.Security.Claims;
using BlazorChat.Extensions;
using Azure.Identity;

namespace BlazorChat.Hubs;

public class ChatHub : Hub
{
    private static ConcurrentDictionary<string, string> groupPasswords = new ConcurrentDictionary<string, string>();
    //private static ConcurrentDictionary<string, HashSet<string>> groupMembers = new ConcurrentDictionary<string, HashSet<string>>();
    private static ConcurrentDictionary<string, List<GroupMember>> groupMembers = new ConcurrentDictionary<string, List<GroupMember>>();
    private static ConcurrentDictionary<string,string> userAvatars = new ConcurrentDictionary<string, string>();

    public class GroupMember
    {
        public string User { get; set; }
        public string Avatar { get; set; }
    }

    public async Task UpdateUserAvatar(string avatar)
    {
        userAvatars[Context.User.Identity?.Name] = avatar;
    }
    public async Task CreateGroup(string groupName, string password)
    {
        if(groupPasswords.TryAdd(groupName, password))
        {
            groupMembers[groupName] = new List<GroupMember>();
            await Clients.Caller.SendAsync("SystemMessage", "System", $"Group {groupName} created successfully.", DateTime.Now);
            // UpdateGroupList when CreatedGroup
            await Clients.All.SendAsync("UpdateGroupList", groupPasswords.Keys.ToList());
            
        }
        else
        {
            await Clients.Caller.SendAsync("SystemMessage", "System", $"Group {groupName} already exists.", DateTime.Now);
        }
    }

    public async Task JoinGroup(string groupName, string password)
    {
        if (groupPasswords.ContainsKey(groupName) && groupPasswords[groupName] == password)
        {
            if (!string.IsNullOrEmpty(Context.User.Identity?.Name))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.Caller.SendAsync("JoinResult", true, groupName);
                await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName, "System", $"{Context.User.Identity?.Name} has joined {groupName}.", TimeOnly.FromDateTime(DateTime.Now));


                groupMembers[groupName].Add(new GroupMember { User = Context.User.Identity?.Name, Avatar = userAvatars[Context.User.Identity?.Name] });
                await Clients.Caller.SendAsync("UpdateGroupMember", groupName, groupMembers[groupName]/*.ToList()*/);
                await Clients.Group(groupName).SendAsync("UpdateGroupMember", groupName, groupMembers[groupName]/*.ToList()*/);
            }  
        }
        else
        {
            await Clients.Caller.SendAsync("JoinResult", false, groupName);
            await Clients.Caller.SendAsync("SystemMessage", "System", "Incorrect group password.", DateTime.Now);
        }
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName, "System", $"{Context.User.Identity?.Name} has left.", TimeOnly.FromDateTime(DateTime.Now));
        await Clients.Caller.SendAsync("LeaveResult",groupName);

        // 删除组成员信息

        var memberToRemove = groupMembers[groupName].FirstOrDefault(m => m.User == Context.User.Identity?.Name);
        if (memberToRemove != null)
        {
            groupMembers[groupName].Remove(memberToRemove);
        }

        await Clients.Caller.SendAsync("UpdateGroupMember", groupName, groupMembers[groupName]/*.ToList()*/);
        await Clients.Group(groupName).SendAsync("UpdateGroupMember", groupName, groupMembers[groupName]/*.ToList()*/);
    }

    //public async Task SendMessageGroup(string groupName, string username, string message)
    //{
    //    await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName ,username, message, TimeOnly.FromDateTime(DateTime.Now));
    //}

    public async Task SendMessageGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName, Context.User.Identity?.Name, userAvatars[Context.User.Identity?.Name], message, TimeOnly.FromDateTime(DateTime.Now));
    }

    // UpdateGroupList when User Connected
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("UpdateGroupList", groupPasswords.Keys.ToList());
        await base.OnConnectedAsync();
    }
}
