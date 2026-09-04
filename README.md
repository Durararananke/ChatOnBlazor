# ChatOnBlazor

ChatOnBlazor is a .NET 8 Blazor chat application. It uses Blazor WebAssembly for the interactive client, ASP.NET Core Identity for accounts, SignalR for real-time chat, SQL Server for identity data, and MudBlazor for the UI.

## Prerequisites

- .NET 8 SDK
- SQL Server LocalDB on Windows, or another SQL Server connection configured as `ConnectionStrings:DefaultConnection`

## Build and test

```powershell
dotnet restore ChatOnBlazor.sln
dotnet build ChatOnBlazor.sln
dotnet test ChatOnBlazor.sln
```

```powershell
dotnet tool install --global dotnet-ef --version 8.0.30
dotnet ef database update --project ChatOnBlazor/ChatOnBlazor.csproj
```

## Run

```powershell
dotnet run --project ChatOnBlazor/ChatOnBlazor.csproj
```

The development registration flow displays an account-confirmation link because the project uses a no-op email sender. Replace `IdentityNoOpEmailSender` with a real email provider before deploying the application.

## Current storage model

Identity accounts are stored in SQL Server. Chat rooms and membership are held in server memory, while message history exists only in each connected client session. This state is cleared on restart and is not shared between server instances. Move it to a database or distributed cache before adding durable history or scaling beyond one process.

## Security behavior

- The SignalR hub requires an authenticated Identity cookie.
- Room passwords are stored as password hashes in memory.
