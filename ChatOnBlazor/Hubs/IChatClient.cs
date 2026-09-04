namespace ChatOnBlazor.Hubs;

public interface IChatClient
{
    Task SystemMessage(string sender, string message, DateTimeOffset timestamp);
    Task UpdateGroupList(string[] groups);
    Task JoinResult(bool succeeded, string groupName);
    Task LeaveResult(string groupName);
    Task ReceiveMessage(string groupName, string sender, string message, DateTimeOffset timestamp);
    Task UpdateGroupMember(string groupName, string[] members);
}
