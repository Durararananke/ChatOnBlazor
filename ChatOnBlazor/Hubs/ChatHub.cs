using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ChatOnBlazor.Hubs;

public class ChatHub:Hub
{
    private static ConcurrentDictionary<string,string> groupPwds=new ConcurrentDictionary<string,string>();
    private static ConcurrentDictionary<string,List<string>> groupMembers=new ConcurrentDictionary<string,List<string>>();
   
    #region Group

    public async Task CreateGroup(string groupName,string groupPwd)
    {
        if(groupPwds.TryAdd(groupName,groupPwd))
        {
            groupMembers[groupName]=new List<string>();
            await Clients.Caller.SendAsync("SysteMessage", "Ghoti", $"Group {groupName} was successfully created.", DateTime.Now);
            await Clients.All.SendAsync("UpdateGroupList", groupPwds.Keys.ToList());
        }
        else
        {
            await Clients.Caller.SendAsync("SysteMessage", "Ghoti", $"Group {groupName} already exists.", DateTime.Now);
        }
    }

    public async Task JoinGroup(string groupName,string groupPwd)
    {
        if (groupPwds.ContainsKey(groupName) && groupPwds[groupName]==groupPwd)
        {
            if(!string.IsNullOrEmpty(Context.User.Identity?.Name))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.Caller.SendAsync("JoinResult", true, groupName);
                await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName, "Ghoti",
                      $"{Context.User.Identity?.Name} has joined {groupName}.", TimeOnly.FromDateTime(DateTime.Now));

                groupMembers[groupName].Add(Context.User.Identity?.Name);
                await Clients.Caller.SendAsync("UpdateGroupMember", groupName, groupMembers[groupName].ToList());
                await Clients.Group(groupName).SendAsync("UpdateGroupMember", groupName, groupMembers[groupName].ToList());
                await Clients.Caller.SendAsync("SysteMessage", "Ghoti", $"Joined {groupName}", DateTime.Now);
            }
            else
            {
                await Clients.Caller.SendAsync("SysteMessage", "Ghoti", "Sorry, Please refresh the webpage", DateTime.Now);
            }
        }
        else
        {
            await Clients.Caller.SendAsync("JoinResult", false, groupName);
            await Clients.Caller.SendAsync("SysteMessage", "Ghoti", "Incorrect group password.", DateTime.Now);
        }
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName, "Ghoti", 
            $"{Context.User.Identity?.Name} has left.", TimeOnly.FromDateTime(DateTime.Now), TimeOnly.FromDateTime(DateTime.Now));
        await Clients.Caller.SendAsync("LeaveResult", groupName);

        groupMembers[groupName].Remove(Context.User.Identity?.Name);
        await Clients.Caller.SendAsync("UpdateGroupMember", groupName, groupMembers[groupName].ToList());
        await Clients.Group(groupName).SendAsync("UpdateGroupMember", groupName, groupMembers[groupName].ToList());
    }

    #endregion

    public async Task SendMessageGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", groupName, Context.User.Identity?.Name, message, TimeOnly.FromDateTime(DateTime.Now));
    }

    // UpdateGroupList when User Connected
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("UpdateGroupList", groupPwds.Keys.ToList());
        await base.OnConnectedAsync();
    }
}
