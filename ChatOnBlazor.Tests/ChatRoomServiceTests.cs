using ChatOnBlazor.Hubs;
using Microsoft.AspNetCore.Identity;

namespace ChatOnBlazor.Tests;

public sealed class ChatRoomServiceTests
{
    [Fact]
    public void CreateRoom_NormalizesNameAndRejectsCaseInsensitiveDuplicate()
    {
        var service = CreateService();

        var created = service.CreateRoom("  General  ", "correct horse battery staple");
        var duplicate = service.CreateRoom("general", "another password");

        Assert.True(created.Succeeded);
        Assert.Equal("General", created.GroupName);
        Assert.False(duplicate.Succeeded);
        Assert.Equal(["General"], service.GetRoomNames());
    }

    [Fact]
    public void JoinRoom_RequiresCorrectPasswordAndTracksMembershipByConnection()
    {
        var service = CreateService();
        service.CreateRoom("General", "secret");

        var rejected = service.JoinRoom("connection-1", "alice@example.com", "General", "wrong");
        var accepted = service.JoinRoom("connection-1", "alice@example.com", "general", "secret");

        Assert.False(rejected.Succeeded);
        Assert.True(accepted.Succeeded);
        Assert.False(service.IsMember("connection-2", "General", out _));
        Assert.True(service.IsMember("connection-1", "General", out var canonicalName));
        Assert.Equal("General", canonicalName);
        Assert.Equal(["alice@example.com"], service.GetMembers("General"));
    }

    [Fact]
    public void RemoveConnection_RemovesUserFromEveryJoinedRoom()
    {
        var service = CreateService();
        service.CreateRoom("General", "one");
        service.CreateRoom("Random", "two");
        service.JoinRoom("connection-1", "alice@example.com", "General", "one");
        service.JoinRoom("connection-1", "alice@example.com", "Random", "two");

        var leftRooms = service.RemoveConnection("connection-1");

        Assert.Equal(["General", "Random"], leftRooms.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Empty(service.GetMembers("General"));
        Assert.Empty(service.GetMembers("Random"));
    }

    [Fact]
    public void Members_AreDeduplicatedAcrossMultipleConnections()
    {
        var service = CreateService();
        service.CreateRoom("General", "secret");
        service.JoinRoom("connection-1", "alice@example.com", "General", "secret");
        service.JoinRoom("connection-2", "alice@example.com", "General", "secret");

        Assert.Equal(["alice@example.com"], service.GetMembers("General"));

        service.RemoveConnection("connection-1");
        Assert.Equal(["alice@example.com"], service.GetMembers("General"));
    }

    [Fact]
    public void CreateRoom_RejectsOversizedInput()
    {
        var service = CreateService();

        var result = service.CreateRoom(
            new string('a', ChatRoomService.MaxGroupNameLength + 1),
            "secret");

        Assert.False(result.Succeeded);
        Assert.Empty(service.GetRoomNames());
    }

    private static ChatRoomService CreateService() => new(new PasswordHasher<string>());
}
