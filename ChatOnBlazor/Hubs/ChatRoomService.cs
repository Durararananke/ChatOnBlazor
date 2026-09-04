using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;

namespace ChatOnBlazor.Hubs;

public sealed class ChatRoomService(IPasswordHasher<string> passwordHasher)
{
    public const int MaxGroupNameLength = 64;
    public const int MaxPasswordLength = 128;
    public const int MaxMessageLength = 4_000;

    private readonly ConcurrentDictionary<string, Room> rooms =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> roomsByConnection =
        new(StringComparer.Ordinal);

    public RoomOperationResult CreateRoom(string? groupName, string? password)
    {
        var normalizedName = groupName?.Trim() ?? string.Empty;
        var validationError = ValidateRoomCredentials(normalizedName, password);
        if (validationError is not null)
        {
            return RoomOperationResult.Failure(normalizedName, validationError);
        }

        var passwordHash = passwordHasher.HashPassword(normalizedName, password!);
        var room = new Room(normalizedName, passwordHash);

        return rooms.TryAdd(normalizedName, room)
            ? RoomOperationResult.Success(normalizedName)
            : RoomOperationResult.Failure(normalizedName, $"Thread {normalizedName} already exists.");
    }

    public RoomOperationResult JoinRoom(
        string connectionId,
        string userName,
        string? groupName,
        string? password)
    {
        var normalizedName = groupName?.Trim() ?? string.Empty;
        if (!rooms.TryGetValue(normalizedName, out var room) || string.IsNullOrEmpty(password))
        {
            return InvalidCredentials(normalizedName);
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            room.Name,
            room.PasswordHash,
            password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return InvalidCredentials(normalizedName);
        }

        room.Members[connectionId] = userName;
        roomsByConnection
            .GetOrAdd(connectionId, static _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase))
            [room.Name] = 0;

        return RoomOperationResult.Success(room.Name);
    }

    public bool LeaveRoom(string connectionId, string? groupName, out string canonicalGroupName)
    {
        canonicalGroupName = groupName?.Trim() ?? string.Empty;
        if (!rooms.TryGetValue(canonicalGroupName, out var room) ||
            !room.Members.TryRemove(connectionId, out _))
        {
            return false;
        }

        canonicalGroupName = room.Name;
        if (roomsByConnection.TryGetValue(connectionId, out var joinedRooms))
        {
            joinedRooms.TryRemove(room.Name, out _);
            if (joinedRooms.IsEmpty)
            {
                roomsByConnection.TryRemove(connectionId, out _);
            }
        }

        return true;
    }

    public string[] RemoveConnection(string connectionId)
    {
        if (!roomsByConnection.TryRemove(connectionId, out var joinedRooms))
        {
            return [];
        }

        var leftRooms = new List<string>();
        foreach (var groupName in joinedRooms.Keys)
        {
            if (rooms.TryGetValue(groupName, out var room) &&
                room.Members.TryRemove(connectionId, out _))
            {
                leftRooms.Add(room.Name);
            }
        }

        return [.. leftRooms];
    }

    public bool IsMember(string connectionId, string? groupName, out string canonicalGroupName)
    {
        canonicalGroupName = groupName?.Trim() ?? string.Empty;
        if (!rooms.TryGetValue(canonicalGroupName, out var room) ||
            !room.Members.ContainsKey(connectionId))
        {
            return false;
        }

        canonicalGroupName = room.Name;
        return true;
    }

    public string[] GetRoomNames() =>
        [.. rooms.Values.Select(static room => room.Name).Order(StringComparer.OrdinalIgnoreCase)];

    public string[] GetMembers(string groupName) =>
        rooms.TryGetValue(groupName, out var room)
            ? [.. room.Members.Values.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)]
            : [];

    private static string? ValidateRoomCredentials(string groupName, string? password)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return "Thread name is required.";
        }

        if (groupName.Length > MaxGroupNameLength)
        {
            return $"Thread names cannot exceed {MaxGroupNameLength} characters.";
        }

        if (string.IsNullOrEmpty(password))
        {
            return "A thread password is required.";
        }

        return password.Length > MaxPasswordLength
            ? $"Thread passwords cannot exceed {MaxPasswordLength} characters."
            : null;
    }

    private static RoomOperationResult InvalidCredentials(string groupName) =>
        RoomOperationResult.Failure(groupName, "Thread name or password is incorrect.");

    private sealed class Room(string name, string passwordHash)
    {
        public string Name { get; } = name;
        public string PasswordHash { get; } = passwordHash;
        public ConcurrentDictionary<string, string> Members { get; } = new(StringComparer.Ordinal);
    }
}

public readonly record struct RoomOperationResult(bool Succeeded, string GroupName, string? Error)
{
    public static RoomOperationResult Success(string groupName) => new(true, groupName, null);
    public static RoomOperationResult Failure(string groupName, string error) => new(false, groupName, error);
}
