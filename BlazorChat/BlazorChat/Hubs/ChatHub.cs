using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Data;

namespace BlazorChat.Hubs;

public class ChatHub : Hub
{
    private static ConcurrentDictionary<string, string> groupPasswords = new ConcurrentDictionary<string, string>();
    
    public async Task CreateGroup(string groupName, string password)
    {
        if(groupPasswords.TryAdd(groupName, password))
        {
            await Clients.Caller.SendAsync("SystemMessage", "System", $"Group {groupName} created successfully.", DateTime.Now);
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
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("JoinResult", true, groupName);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName, "System", $"{Context.User.Identity.Name} has joined {groupName}.", TimeOnly.FromDateTime(DateTime.Now));
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
        await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName, "System", $"{Context.ConnectionId} has left.", TimeOnly.FromDateTime(DateTime.Now));
        await Clients.Caller.SendAsync("LeaveResult",groupName);
       
    }

    public async Task SendMessageGroup(string groupName, string username, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName ,username, message, TimeOnly.FromDateTime(DateTime.Now));
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("UpdateGroupList", groupPasswords.Keys.ToList());
        await base.OnConnectedAsync();
    }
}
