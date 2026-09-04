using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatOnBlazor.Hubs;

[Authorize]
public sealed class ChatHub(ChatRoomService chatRooms) : Hub<IChatClient>
{
    private const string SystemSender = "Ghoti";

    public async Task CreateGroup(string? groupName, string? groupPassword)
    {
        var result = chatRooms.CreateRoom(groupName, groupPassword);
        if (!result.Succeeded)
        {
            await Clients.Caller.SystemMessage(SystemSender, result.Error!, DateTimeOffset.UtcNow);
            return;
        }

        await Clients.Caller.SystemMessage(
            SystemSender,
            $"Thread {result.GroupName} was successfully created.",
            DateTimeOffset.UtcNow);
        await Clients.All.UpdateGroupList(chatRooms.GetRoomNames());
    }

    public async Task JoinGroup(string? groupName, string? groupPassword)
    {
        var userName = GetUserName();
        var result = chatRooms.JoinRoom(Context.ConnectionId, userName, groupName, groupPassword);
        if (!result.Succeeded)
        {
            await Clients.Caller.JoinResult(false, result.GroupName);
            await Clients.Caller.SystemMessage(SystemSender, result.Error!, DateTimeOffset.UtcNow);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, result.GroupName);
        await Clients.Caller.JoinResult(true, result.GroupName);
        await Clients.Group(result.GroupName).ReceiveMessage(
            result.GroupName,
            SystemSender,
            $"{userName} has joined {result.GroupName}.",
            DateTimeOffset.UtcNow);
        await BroadcastMembers(result.GroupName);
        await Clients.Caller.SystemMessage(SystemSender, $"Joined {result.GroupName}", DateTimeOffset.UtcNow);
    }

    public async Task LeaveGroup(string? groupName)
    {
        if (!chatRooms.LeaveRoom(Context.ConnectionId, groupName, out var canonicalGroupName))
        {
            await Clients.Caller.SystemMessage(SystemSender, "You have not joined that thread.", DateTimeOffset.UtcNow);
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, canonicalGroupName);
        await Clients.Group(canonicalGroupName).ReceiveMessage(
            canonicalGroupName,
            SystemSender,
            $"{GetUserName()} has left.",
            DateTimeOffset.UtcNow);
        await Clients.Caller.LeaveResult(canonicalGroupName);
        await BroadcastMembers(canonicalGroupName);
    }

    public async Task SendMessageGroup(string? groupName, string? message)
    {
        if (!chatRooms.IsMember(Context.ConnectionId, groupName, out var canonicalGroupName))
        {
            throw new HubException("Join the thread before sending messages.");
        }

        var normalizedMessage = message?.Trim() ?? string.Empty;
        if (normalizedMessage.Length is 0 or > ChatRoomService.MaxMessageLength)
        {
            throw new HubException($"Messages must contain 1 to {ChatRoomService.MaxMessageLength} characters.");
        }

        await Clients.Group(canonicalGroupName).ReceiveMessage(
            canonicalGroupName,
            GetUserName(),
            normalizedMessage,
            DateTimeOffset.UtcNow);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.UpdateGroupList(chatRooms.GetRoomNames());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name ?? "A user";
        foreach (var groupName in chatRooms.RemoveConnection(Context.ConnectionId))
        {
            await Clients.Group(groupName).ReceiveMessage(
                groupName,
                SystemSender,
                $"{userName} disconnected.",
                DateTimeOffset.UtcNow);
            await BroadcastMembers(groupName);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastMembers(string groupName) =>
        Clients.Group(groupName).UpdateGroupMember(groupName, chatRooms.GetMembers(groupName));

    private string GetUserName() =>
        Context.User?.Identity?.Name ?? throw new HubException("An authenticated user is required.");
}
