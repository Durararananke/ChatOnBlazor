using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BlazorChat.Hubs;

public class ChatHub : Hub
{
    private static ConcurrentDictionary<string, string> groupPasswords = new ConcurrentDictionary<string, string>();

    public async Task CreateGroup(string groupName, string password)
    {
        if(groupPasswords.TryAdd(groupName, password))
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "System", $"Group {groupName} created successfully.");
            await Clients.All.SendAsync("UpdateGroupList", groupPasswords.Keys.ToList());
        }
        else
        {
            await Clients.Caller.SendAsync("ReceiveMessage", "System", $"Group {groupName} already exists.");
        }
    }

    public async Task JoinGroup(string groupName, string password)
    {
        if (groupPasswords.ContainsKey(groupName) && groupPasswords[groupName] == password)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Caller.SendAsync("JoinResult", true, groupName);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", "System", $"{Context.ConnectionId} has joined {groupName}.");
        }
        else
        {
            await Clients.Caller.SendAsync("JoinResult", false, groupName);
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Incorrect group password.");
        }
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeaveResult");
        await Clients.Group(groupName).SendAsync("ReceiveMessage", "System", $"{Context.ConnectionId} has left.");
    }

    public async Task SendMessageGroup(string groupName, string username, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", username, message);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("UpdateGroupList", groupPasswords.Keys.ToList());
        await base.OnConnectedAsync();
    }
}
