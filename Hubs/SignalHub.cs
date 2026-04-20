using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BlogApiPrev.Hubs
{
    public class SignalHub : Hub
    {
        // In-memory user connection map (username → connectionId)
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();

        // Register the username with the connection
        public Task Register(string username)
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                _userConnections[username] = Context.ConnectionId;
            }
            return Task.CompletedTask;
        }

        // Send a private message from one user to another
        public async Task SendPrivateMessage(string toUsername, string fromUsername, string message)
        {
            if (_userConnections.TryGetValue(toUsername, out var toConnectionId))
            {
                await Clients.Client(toConnectionId).SendAsync("ReceivePrivateMessage", new
                {
                    from = fromUsername,
                    message = message
                });
            }
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Optional cleanup: remove disconnected user's entry
            var disconnected = _userConnections.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
            if (!string.IsNullOrEmpty(disconnected.Key))
            {
                _userConnections.TryRemove(disconnected.Key, out _);
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}