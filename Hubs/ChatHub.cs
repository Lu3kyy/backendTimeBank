// using Microsoft.AspNetCore.SignalR;
// using System.Collections.Concurrent;

// public class ChatHub : Hub
// {
//     // 🔑 userId -> multiple connectionIds (handles multiple tabs/devices)
//     private static readonly ConcurrentDictionary<string, HashSet<string>> _connections
//         = new();

//     // 🔑 connectionId -> userId (for cleanup)
//     private static readonly ConcurrentDictionary<string, string> _reverseLookup
//         = new();

//     // =========================
//     // CONNECT / DISCONNECT
//     // =========================

//     public override async Task OnConnectedAsync()
//     {
//         var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

//         if (string.IsNullOrEmpty(userId))
//         {
//             throw new Exception("UserId is required");
//         }

//         var connectionId = Context.ConnectionId;

//         // add connection
//         _connections.AddOrUpdate(userId,
//             _ => new HashSet<string> { connectionId },
//             (_, existing) =>
//             {
//                 lock (existing)
//                 {
//                     existing.Add(connectionId);
//                 }
//                 return existing;
//             });

//         _reverseLookup[connectionId] = userId;

//         await base.OnConnectedAsync();
//     }

//     public override async Task OnDisconnectedAsync(Exception? exception)
//     {
//         var connectionId = Context.ConnectionId;

//         if (_reverseLookup.TryRemove(connectionId, out var userId))
//         {
//             if (_connections.TryGetValue(userId, out var connections))
//             {
//                 lock (connections)
//                 {
//                     connections.Remove(connectionId);

//                     if (connections.Count == 0)
//                     {
//                         _connections.TryRemove(userId, out _);
//                     }
//                 }
//             }
//         }

//         await base.OnDisconnectedAsync(exception);
//     }

//     // =========================
//     // SEND MESSAGE
//     // =========================

//     public async Task PostMessage(Message message)
//     {
//         message.SentTime = DateTime.UtcNow;

//         // send to recipient
//         if (_connections.TryGetValue(message.RecipientId, out var recipientConnections))
//         {
//             foreach (var connId in recipientConnections)
//             {
//                 await Clients.Client(connId)
//                     .SendAsync("ReceiveMessage", message);
//             }
//         }

//         // send back to sender (so their UI updates)
//         if (_connections.TryGetValue(message.SenderId, out var senderConnections))
//         {
//             foreach (var connId in senderConnections)
//             {
//                 await Clients.Client(connId)
//                     .SendAsync("ReceiveMessage", message);
//             }
//         }
//     }

//     // =========================
//     // HISTORY (stub)
//     // =========================

//     public async Task RetrieveMessageHistory(string userId)
//     {
//         // ⚠️ Replace with real DB later
//         var history = new List<Message>();

//         await Clients.Caller.SendAsync("ReceiveHistory", history);
//     }
// }

// // =========================
// // MESSAGE MODEL
// // =========================

// public class Message
// {
//     public string SenderId { get; set; } = "";
//     public string RecipientId { get; set; } = "";
//     public string Content { get; set; } = "";
//     public DateTime SentTime { get; set; }
// }