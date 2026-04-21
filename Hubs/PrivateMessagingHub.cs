using BlogApiPrev.Context;
using BlogApiPrev.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace SignalR.Hubs
{
    public class PrivateMessageHub : Hub
    {
        private const int DirectMessageHelpPostId = 0;

        // In-memory user connection map (username → connectionId)
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        private static readonly ConcurrentDictionary<string, string> _connectionUsers = new();

        private readonly DataContext _dataContext;

        public PrivateMessageHub(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        // Register the username with the connection
        public Task Register(string username)
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                var normalizedUsername = username.Trim();
                _userConnections[normalizedUsername] = Context.ConnectionId;
                _connectionUsers[Context.ConnectionId] = normalizedUsername;
            }

            return Task.CompletedTask;
        }

        // Send a private message from one user to another
        public async Task SendPrivateMessage(string toUsername, string fromUsername, string message)
        {
            if (string.IsNullOrWhiteSpace(toUsername) || string.IsNullOrWhiteSpace(fromUsername) || string.IsNullOrWhiteSpace(message))
            {
                throw new HubException("Sender, recipient, and message are required.");
            }

            var normalizedToUsername = toUsername.Trim();
            var normalizedFromUsername = fromUsername.Trim();
            var trimmedMessage = message.Trim();

            var sender = await _dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Username == normalizedFromUsername);
            var recipient = await _dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Username == normalizedToUsername);

            if (sender == null || recipient == null)
            {
                throw new HubException("Sender or recipient was not found.");
            }

            var thread = await GetOrCreateDirectMessageThreadAsync(sender.Id, recipient.Id);

            var chatMessage = new ChatMessageModel
            {
                ChatThreadId = thread.Id,
                SenderUserId = sender.Id,
                Message = trimmedMessage,
                SentAtUtc = DateTime.UtcNow,
                ReadAtUtc = null
            };

            await _dataContext.ChatMessages.AddAsync(chatMessage);
            await _dataContext.SaveChangesAsync();

            var payload = new PrivateMessagePayload
            {
                ChatThreadId = thread.Id,
                From = normalizedFromUsername,
                To = normalizedToUsername,
                Message = trimmedMessage,
                SentAtUtc = chatMessage.SentAtUtc,
                ReadAtUtc = chatMessage.ReadAtUtc
            };

            if (_userConnections.TryGetValue(normalizedToUsername, out var toConnectionId))
            {
                await Clients.Client(toConnectionId).SendAsync("ReceivePrivateMessage", payload);
            }

            await Clients.Caller.SendAsync("ReceivePrivateMessage", payload);
        }

        public async Task<List<PrivateMessagePayload>> GetConversation(string withUsername)
        {
            if (string.IsNullOrWhiteSpace(withUsername))
            {
                return [];
            }

            if (!_connectionUsers.TryGetValue(Context.ConnectionId, out var currentUsername))
            {
                throw new HubException("Call Register before requesting conversation history.");
            }

            var normalizedWithUsername = withUsername.Trim();
            var currentUser = await _dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Username == currentUsername);
            var otherUser = await _dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Username == normalizedWithUsername);

            if (currentUser == null || otherUser == null)
            {
                return [];
            }

            var thread = await _dataContext.ChatThreads
                .AsNoTracking()
                .Where(t => t.HelpPostId == DirectMessageHelpPostId &&
                       ((t.InitiatorUserId == currentUser.Id && t.RecipientUserId == otherUser.Id) ||
                        (t.InitiatorUserId == otherUser.Id && t.RecipientUserId == currentUser.Id)))
                .OrderByDescending(t => t.StartedAtUtc)
                .FirstOrDefaultAsync();

            if (thread == null)
            {
                return [];
            }

            var unreadMessages = await _dataContext.ChatMessages
                .Where(message => message.ChatThreadId == thread.Id &&
                    message.SenderUserId != currentUser.Id &&
                    message.ReadAtUtc == null)
                .ToListAsync();

            if (unreadMessages.Count > 0)
            {
                var readAtUtc = DateTime.UtcNow;
                foreach (var unreadMessage in unreadMessages)
                {
                    unreadMessage.ReadAtUtc = readAtUtc;
                }

                await _dataContext.SaveChangesAsync();
            }

            var messages = await _dataContext.ChatMessages
                .AsNoTracking()
                .Where(message => message.ChatThreadId == thread.Id)
                .OrderBy(message => message.SentAtUtc)
                .ToListAsync();

            return messages.Select(message => new PrivateMessagePayload
            {
                ChatThreadId = thread.Id,
                From = message.SenderUserId == currentUser.Id ? currentUsername : normalizedWithUsername,
                To = message.SenderUserId == currentUser.Id ? normalizedWithUsername : currentUsername,
                Message = message.Message,
                SentAtUtc = message.SentAtUtc,
                ReadAtUtc = message.ReadAtUtc
            }).ToList();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Optional cleanup: remove disconnected user's entry
            if (_connectionUsers.TryRemove(Context.ConnectionId, out var username))
            {
                _userConnections.TryRemove(username, out _);
            }

            return base.OnDisconnectedAsync(exception);
        }

        private async Task<ChatThreadModel> GetOrCreateDirectMessageThreadAsync(int senderUserId, int recipientUserId)
        {
            var thread = await _dataContext.ChatThreads
                .Where(t => t.HelpPostId == DirectMessageHelpPostId &&
                       ((t.InitiatorUserId == senderUserId && t.RecipientUserId == recipientUserId) ||
                        (t.InitiatorUserId == recipientUserId && t.RecipientUserId == senderUserId)))
                .OrderByDescending(t => t.StartedAtUtc)
                .FirstOrDefaultAsync();

            if (thread != null)
            {
                return thread;
            }

            thread = new ChatThreadModel
            {
                HelpPostId = DirectMessageHelpPostId,
                InitiatorUserId = senderUserId,
                RecipientUserId = recipientUserId,
                Status = "Active",
                StartedAtUtc = DateTime.UtcNow
            };

            await _dataContext.ChatThreads.AddAsync(thread);
            await _dataContext.SaveChangesAsync();
            return thread;
        }
    }

    public class PrivateMessagePayload
    {
        public int ChatThreadId { get; set; }
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; }
        public DateTime? ReadAtUtc { get; set; }
    }
}