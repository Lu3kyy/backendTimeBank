using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BlogApiPrev.Context;
using BlogApiPrev.Models;
using BlogApiPrev.Models.DTOS;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BlogApiPrev.Services
{
    public class UserServices
    {
        private const int DirectMessageHelpPostId = 0;

        private static readonly string[] AllowedHelpCategories =
        [
            "home",
            "learning",
            "garden",
            "pet",
            "creative",
            "fitness",
            "other"
        ];

        private static readonly string[] AllowedHelpPostTypes = ["offer", "request"];

        private readonly DataContext _dataContext;
        private readonly IConfiguration _config;

        public UserServices(DataContext dataContext, IConfiguration config)
        {
            _dataContext = dataContext;
            _config = config;
        }

        public async Task<bool> CreateAccount(UserDTO newUser)
        {
            var username = newUser.Username.Trim();
            if (await DoesUserExist(username))
            {
                return false;
            }

            var encryptedPassword = HashPassword(newUser.Password);
            UserModel user = new()
            {
                Username = username,
                Hash = encryptedPassword.Hash,
                Salt = encryptedPassword.Salt,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _dataContext.Users.AddAsync(user);
            return await _dataContext.SaveChangesAsync() != 0;
        }

        private async Task<bool> DoesUserExist(string username)
        {
            return await _dataContext.Users.AnyAsync(user => user.Username == username);
        }

        private static PasswordDTO HashPassword(string password)
        {
            byte[] SaltBytes = RandomNumberGenerator.GetBytes(64);

            string salt = Convert.ToBase64String(SaltBytes);

            string hash;

            using (var derivedBytes = new Rfc2898DeriveBytes(password, SaltBytes, 310000, HashAlgorithmName.SHA256))
            {
                hash = Convert.ToBase64String(derivedBytes.GetBytes(32));
            }

            return new PasswordDTO
            {
                Salt = salt,
                Hash = hash
            };
        }

        public async Task<AuthResponseDTO?> Login(UserDTO user)
        {
            var username = user.Username.Trim();
            UserModel? currentUser = await GetUserInfoByUsernameAsync(username);

            if (currentUser == null)
            {
                return null;
            }

            if (!VerifyPassword(user.Password, currentUser.Salt, currentUser.Hash))
            {
                return null;
            }

            var expiresAtUtc = DateTime.UtcNow.AddHours(2);
            var token = GenerateJWT(currentUser, expiresAtUtc);

            return new AuthResponseDTO
            {
                UserId = currentUser.Id,
                Username = currentUser.Username,
                Token = token,
                ExpiresAtUtc = expiresAtUtc
            };
        }

        private string GenerateJWT(UserModel user, DateTime expiresAtUtc)
        {
            var secret = _config["JWT:Key"] ?? throw new InvalidOperationException("JWT key is missing.");
            var issuer = _config["JWT:Issuer"] ?? "http://localhost:5000";
            var audience = _config["JWT:Audience"] ?? "http://localhost:5000";

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username)
            };

            var tokenOptions = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }

        private static bool VerifyPassword(string password, string salt, string hash)
        {
            byte[] saltByte = Convert.FromBase64String(salt);

            string checkHash;

            using(var derivedBytes = new Rfc2898DeriveBytes(password, saltByte, 310000, HashAlgorithmName.SHA256))
            {
                checkHash = Convert.ToBase64String(derivedBytes.GetBytes(32));
                return hash == checkHash;
            }
        }

        public async Task<UserModel?> GetUserInfoByUsernameAsync(string username) =>
            await _dataContext.Users.SingleOrDefaultAsync(user => user.Username == username);

        public async Task<UserInfoDTO?> GetUserProfileByIdAsync(int userId)
        {
            var currentUser = await _dataContext.Users.SingleOrDefaultAsync(user => user.Id == userId);

            if (currentUser == null)
            {
                return null;
            }

            return MapToUserInfo(currentUser);
        }

        public async Task<List<UserInfoDTO>> GetProfilesAsync(
            string? search,
            int skip = 0,
            int take = 20,
            bool random = false,
            bool onlyComplete = false,
            string? city = null,
            double? latitude = null,
            double? longitude = null,
            double? radiusKm = null)
        {
            skip = Math.Max(skip, 0);

            IQueryable<UserModel> query = _dataContext.Users.AsNoTracking();

            query = onlyComplete
                ? query.Where(user =>
                    !string.IsNullOrWhiteSpace(user.Name) &&
                    !string.IsNullOrWhiteSpace(user.ProfilePictureUrl) &&
                    !string.IsNullOrWhiteSpace(user.Description))
                : query.Where(user =>
                    !string.IsNullOrWhiteSpace(user.Name) ||
                    !string.IsNullOrWhiteSpace(user.ProfilePictureUrl) ||
                    !string.IsNullOrWhiteSpace(user.Description));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLower();
                query = query.Where(user =>
                    user.Username.ToLower().Contains(normalizedSearch) ||
                    (user.Name != null && user.Name.ToLower().Contains(normalizedSearch)) ||
                    (user.Description != null && user.Description.ToLower().Contains(normalizedSearch)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var normalizedCity = city.Trim().ToLower();
                query = query.Where(user =>
                    (user.Name != null && user.Name.ToLower().Contains(normalizedCity)) ||
                    (user.Description != null && user.Description.ToLower().Contains(normalizedCity)));
            }

            var useDistanceSort = latitude.HasValue && longitude.HasValue;

            if (useDistanceSort)
            {
                query = query.OrderByDescending(user => user.UpdatedAtUtc);
            }
            else
            {
                query = random
                    ? query.OrderBy(user => Guid.NewGuid())
                    : query.OrderByDescending(user => user.UpdatedAtUtc);

                query = query.Skip(skip);

                if (take > 0)
                {
                    take = Math.Clamp(take, 1, 100);
                    query = query.Take(take);
                }
            }

            var users = await query.ToListAsync();

            var mapped = users.Select(MapToUserInfo).ToList();

            if (useDistanceSort)
            {
                var latestLocationsByUserId = await _dataContext.HelpPosts
                    .AsNoTracking()
                    .Where(post =>
                        post.Latitude.HasValue &&
                        post.Longitude.HasValue &&
                        users.Select(user => user.Id).Contains(post.CreatedByUserId))
                    .OrderByDescending(post => post.CreatedAtUtc)
                    .ToListAsync();

                var firstLocationPerUser = latestLocationsByUserId
                    .GroupBy(post => post.CreatedByUserId)
                    .ToDictionary(group => group.Key, group => group.First());

                foreach (var profile in mapped)
                {
                    if (firstLocationPerUser.TryGetValue(profile.Id, out var locationPost))
                    {
                        profile.DistanceKm = CalculateDistanceKm(
                            latitude!.Value,
                            longitude!.Value,
                            locationPost.Latitude!.Value,
                            locationPost.Longitude!.Value);
                    }
                }

                if (radiusKm.HasValue)
                {
                    mapped = mapped
                        .Where(profile => profile.DistanceKm.HasValue && profile.DistanceKm.Value <= radiusKm.Value)
                        .ToList();
                }

                mapped = mapped
                    .OrderBy(profile => profile.DistanceKm ?? double.MaxValue)
                    .ThenByDescending(profile => users.First(user => user.Id == profile.Id).UpdatedAtUtc)
                    .ToList();

                mapped = mapped.Skip(skip).ToList();

                if (take > 0)
                {
                    take = Math.Clamp(take, 1, 100);
                    mapped = mapped.Take(take).ToList();
                }
            }

            return mapped;
        }

        public async Task<UserInfoDTO?> CreateProfileAsync(int userId, ProfileUpsertDTO profile)
        {
            var currentUser = await _dataContext.Users.SingleOrDefaultAsync(user => user.Id == userId);

            if (currentUser == null)
            {
                return null;
            }

            currentUser.Name = profile.Name.Trim();
            currentUser.Description = profile.Description?.Trim();
            currentUser.UpdatedAtUtc = DateTime.UtcNow;

            _dataContext.Users.Update(currentUser);
            await _dataContext.SaveChangesAsync();
            return MapToUserInfo(currentUser);
        }

        public async Task<UserInfoDTO?> UpdateProfileAsync(int userId, ProfileUpdateDTO profile)
        {
            var currentUser = await _dataContext.Users.SingleOrDefaultAsync(user => user.Id == userId);

            if (currentUser == null)
            {
                return null;
            }

            if (profile.Name is not null)
            {
                currentUser.Name = profile.Name.Trim();
            }

            if (profile.Description is not null)
            {
                currentUser.Description = profile.Description.Trim();
            }

            currentUser.UpdatedAtUtc = DateTime.UtcNow;

            _dataContext.Users.Update(currentUser);
            await _dataContext.SaveChangesAsync();
            return MapToUserInfo(currentUser);
        }

        public async Task<UserInfoDTO?> SetProfilePictureUrlAsync(int userId, string pictureUrl)
        {
            var currentUser = await _dataContext.Users.SingleOrDefaultAsync(user => user.Id == userId);

            if (currentUser == null)
            {
                return null;
            }

            currentUser.ProfilePictureUrl = pictureUrl.Trim();
            currentUser.UpdatedAtUtc = DateTime.UtcNow;

            _dataContext.Users.Update(currentUser);
            await _dataContext.SaveChangesAsync();
            return MapToUserInfo(currentUser);
        }

        public List<HelpCategoryDTO> GetHelpCategories()
        {
            return AllowedHelpCategories
                .Select(category => new HelpCategoryDTO { Category = category })
                .ToList();
        }

        public async Task<HelpPostDTO?> CreateHelpPostAsync(int userId, HelpPostCreateDTO post)
        {
            var user = await _dataContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return null;
            }

            var normalizedCategory = NormalizeHelpCategory(post.Category);
            var normalizedPostType = NormalizeHelpPostType(post.PostType);
            if (normalizedCategory == null || normalizedPostType == null)
            {
                return null;
            }

            var newPost = new HelpPostModel
            {
                CreatedByUserId = userId,
                Category = normalizedCategory,
                PostType = normalizedPostType,
                Subcategory = string.Empty,
                Title = post.Title.Trim(),
                Description = post.Description.Trim(),
                Latitude = post.Latitude,
                Longitude = post.Longitude,
                IsOpen = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _dataContext.HelpPosts.AddAsync(newPost);
            await _dataContext.SaveChangesAsync();

            return new HelpPostDTO
            {
                Id = newPost.Id,
                CreatedByUserId = newPost.CreatedByUserId,
                CreatorName = string.IsNullOrWhiteSpace(user.Name) ? user.Username : user.Name,
                CreatorProfilePictureUrl = user.ProfilePictureUrl,
                Category = newPost.Category,
                PostType = newPost.PostType,
                Title = newPost.Title,
                Description = newPost.Description,
                DistanceKm = null,
                IsOpen = newPost.IsOpen,
                CreatedAtUtc = newPost.CreatedAtUtc
            };
        }

        public async Task<List<HelpPostDTO>> GetHelpPostsAsync(string? category, string? postType, double? latitude, double? longitude, double? radiusKm)
        {
            IQueryable<HelpPostModel> query = _dataContext.HelpPosts.Where(p => p.IsOpen);

            if (!string.IsNullOrWhiteSpace(category))
            {
                var normalizedCategory = NormalizeHelpCategory(category);
                if (normalizedCategory == null)
                {
                    return [];
                }

                query = query.Where(p => p.Category == normalizedCategory);
            }

            if (!string.IsNullOrWhiteSpace(postType))
            {
                var normalizedPostType = NormalizeHelpPostType(postType);
                if (normalizedPostType == null)
                {
                    return [];
                }

                query = query.Where(p => p.PostType == normalizedPostType);
            }

            var posts = await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync();

            var creatorIds = posts.Select(p => p.CreatedByUserId).Distinct().ToList();
            var creators = await _dataContext.Users
                .Where(u => creatorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            var result = posts.Select(p =>
            {
                creators.TryGetValue(p.CreatedByUserId, out var creator);
                double? distance = latitude.HasValue && longitude.HasValue && p.Latitude.HasValue && p.Longitude.HasValue
                    ? CalculateDistanceKm(latitude.Value, longitude.Value, p.Latitude.Value, p.Longitude.Value)
                    : null;

                return new HelpPostDTO
                {
                    Id = p.Id,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatorName = creator == null ? "Unknown" : (string.IsNullOrWhiteSpace(creator.Name) ? creator.Username : creator.Name),
                    CreatorProfilePictureUrl = creator?.ProfilePictureUrl,
                    Category = p.Category,
                    PostType = p.PostType,
                    Title = p.Title,
                    Description = p.Description,
                    DistanceKm = distance,
                    IsOpen = p.IsOpen,
                    CreatedAtUtc = p.CreatedAtUtc
                };
            }).ToList();

            if (radiusKm.HasValue)
            {
                result = result.Where(p => p.DistanceKm.HasValue && p.DistanceKm.Value <= radiusKm.Value).ToList();
            }

            result = latitude.HasValue && longitude.HasValue
                ? result.OrderBy(p => p.DistanceKm ?? double.MaxValue).ThenByDescending(p => p.CreatedAtUtc).ToList()
                : result.OrderByDescending(p => p.CreatedAtUtc).ToList();

            return result;
        }

        public async Task<List<HelpPostDTO>> GetMyHelpPostsAsync(int userId)
        {
            var posts = await _dataContext.HelpPosts
                .Where(p => p.CreatedByUserId == userId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToListAsync();

            var user = await _dataContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            var creatorName = user == null ? "Unknown" : (string.IsNullOrWhiteSpace(user.Name) ? user.Username : user.Name);
            var creatorProfilePictureUrl = user?.ProfilePictureUrl;

            return posts.Select(p => new HelpPostDTO
            {
                Id = p.Id,
                CreatedByUserId = p.CreatedByUserId,
                CreatorName = creatorName,
                CreatorProfilePictureUrl = creatorProfilePictureUrl,
                Category = p.Category,
                PostType = p.PostType,
                Title = p.Title,
                Description = p.Description,
                DistanceKm = null,
                IsOpen = p.IsOpen,
                CreatedAtUtc = p.CreatedAtUtc
            }).ToList();
        }

        private static string? NormalizeHelpCategory(string category)
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            return AllowedHelpCategories.Contains(normalizedCategory) ? normalizedCategory : null;
        }

        private static string? NormalizeHelpPostType(string postType)
        {
            var normalizedPostType = postType.Trim().ToLowerInvariant();
            return AllowedHelpPostTypes.Contains(normalizedPostType) ? normalizedPostType : null;
        }

        public async Task<bool> CloseHelpPostAsync(int userId, int postId)
        {
            var post = await _dataContext.HelpPosts.SingleOrDefaultAsync(p => p.Id == postId);
            if (post == null || post.CreatedByUserId != userId)
            {
                return false;
            }

            if (!post.IsOpen)
            {
                return true;
            }

            post.IsOpen = false;
            post.ClosedAtUtc = DateTime.UtcNow;
            await _dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<ChatThreadDTO?> StartChatAsync(int currentUserId, StartChatDTO input)
        {
            var post = await _dataContext.HelpPosts.SingleOrDefaultAsync(p => p.Id == input.HelpPostId && p.IsOpen);
            if (post == null || post.CreatedByUserId == currentUserId)
            {
                return null;
            }

            var existingThread = await _dataContext.ChatThreads
                .Where(t => t.HelpPostId == input.HelpPostId &&
                       ((t.InitiatorUserId == currentUserId && t.RecipientUserId == post.CreatedByUserId) ||
                        (t.InitiatorUserId == post.CreatedByUserId && t.RecipientUserId == currentUserId)) &&
                       t.Status == "Active")
                .OrderByDescending(t => t.StartedAtUtc)
                .FirstOrDefaultAsync();

            ChatThreadModel thread;
            if (existingThread != null)
            {
                thread = existingThread;
            }
            else
            {
                thread = new ChatThreadModel
                {
                    HelpPostId = post.Id,
                    InitiatorUserId = currentUserId,
                    RecipientUserId = post.CreatedByUserId,
                    Status = "Active",
                    StartedAtUtc = DateTime.UtcNow
                };

                await _dataContext.ChatThreads.AddAsync(thread);
                await _dataContext.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(input.Message))
            {
                await _dataContext.ChatMessages.AddAsync(new ChatMessageModel
                {
                    ChatThreadId = thread.Id,
                    SenderUserId = currentUserId,
                    Message = input.Message.Trim(),
                    SentAtUtc = DateTime.UtcNow
                });
                await _dataContext.SaveChangesAsync();
            }

            return await MapChatThreadAsync(thread);
        }

        public async Task<List<ChatThreadDTO>> GetMyChatsAsync(int userId)
        {
            var threads = await _dataContext.ChatThreads
                .Where(t => t.InitiatorUserId == userId || t.RecipientUserId == userId)
                .OrderByDescending(t => t.StartedAtUtc)
                .ToListAsync();

            var result = new List<ChatThreadDTO>();
            foreach (var thread in threads)
            {
                var mapped = await MapChatThreadAsync(thread);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }

            return result;
        }

        public async Task<List<DmInboxItemDTO>> GetDirectMessageInboxAsync(int userId)
        {
            var threads = await _dataContext.ChatThreads
                .AsNoTracking()
                .Where(thread => thread.HelpPostId == DirectMessageHelpPostId &&
                    (thread.InitiatorUserId == userId || thread.RecipientUserId == userId))
                .ToListAsync();

            if (threads.Count == 0)
            {
                return [];
            }

            var threadIds = threads.Select(thread => thread.Id).ToList();
            var messages = await _dataContext.ChatMessages
                .AsNoTracking()
                .Where(message => threadIds.Contains(message.ChatThreadId))
                .OrderByDescending(message => message.SentAtUtc)
                .ToListAsync();

            var latestMessageByThreadId = messages
                .GroupBy(message => message.ChatThreadId)
                .ToDictionary(group => group.Key, group => group.First());

            var unreadCountByThreadId = messages
                .GroupBy(message => message.ChatThreadId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(message => message.SenderUserId != userId && message.ReadAtUtc == null));

            var participantIds = threads
                .SelectMany(thread => new[] { thread.InitiatorUserId, thread.RecipientUserId })
                .Distinct()
                .ToList();

            var users = await _dataContext.Users
                .AsNoTracking()
                .Where(user => participantIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id);

            var inbox = threads.Select(thread =>
            {
                var otherUserId = thread.InitiatorUserId == userId ? thread.RecipientUserId : thread.InitiatorUserId;
                users.TryGetValue(otherUserId, out var otherUser);
                latestMessageByThreadId.TryGetValue(thread.Id, out var latestMessage);

                var lastMessageSenderUsername = string.Empty;
                if (latestMessage != null && users.TryGetValue(latestMessage.SenderUserId, out var lastMessageSender))
                {
                    lastMessageSenderUsername = lastMessageSender.Username;
                }

                return new DmInboxItemDTO
                {
                    ThreadId = thread.Id,
                    OtherUserId = otherUserId,
                    OtherUsername = otherUser?.Username ?? string.Empty,
                    OtherDisplayName = otherUser == null ? "Unknown" : (string.IsNullOrWhiteSpace(otherUser.Name) ? otherUser.Username : otherUser.Name),
                    OtherProfilePictureUrl = otherUser?.ProfilePictureUrl,
                    LastMessagePreview = latestMessage?.Message ?? string.Empty,
                    LastMessageAtUtc = latestMessage?.SentAtUtc ?? thread.StartedAtUtc,
                    LastMessageFromUsername = lastMessageSenderUsername,
                    UnreadCount = unreadCountByThreadId.GetValueOrDefault(thread.Id, 0)
                };
            })
            .OrderByDescending(item => item.LastMessageAtUtc)
            .ToList();

            return inbox;
        }

        public async Task<int> GetDirectMessageUnreadCountAsync(int userId)
        {
            var threadIds = await _dataContext.ChatThreads
                .AsNoTracking()
                .Where(thread => thread.HelpPostId == DirectMessageHelpPostId &&
                    (thread.InitiatorUserId == userId || thread.RecipientUserId == userId))
                .Select(thread => thread.Id)
                .ToListAsync();

            if (threadIds.Count == 0)
            {
                return 0;
            }

            return await _dataContext.ChatMessages
                .AsNoTracking()
                .Where(message => threadIds.Contains(message.ChatThreadId) &&
                    message.SenderUserId != userId &&
                    message.ReadAtUtc == null)
                .CountAsync();
        }

        public async Task<List<DmMessageDTO>?> GetDirectMessageConversationAsync(int currentUserId, string otherUsername)
        {
            var otherUser = await _dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Username == otherUsername.Trim());

            if (otherUser == null)
            {
                return null;
            }

            var thread = await GetDirectMessageThreadAsync(currentUserId, otherUser.Id);
            if (thread == null)
            {
                return [];
            }

            var messages = await _dataContext.ChatMessages
                .AsNoTracking()
                .Where(message => message.ChatThreadId == thread.Id)
                .OrderBy(message => message.SentAtUtc)
                .ToListAsync();

            var participantIds = messages
                .Select(message => message.SenderUserId)
                .Append(currentUserId)
                .Append(otherUser.Id)
                .Distinct()
                .ToList();

            var users = await _dataContext.Users
                .AsNoTracking()
                .Where(user => participantIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id);

            return messages.Select(message =>
            {
                users.TryGetValue(message.SenderUserId, out var sender);

                return new DmMessageDTO
                {
                    Id = message.Id,
                    ChatThreadId = message.ChatThreadId,
                    SenderUserId = message.SenderUserId,
                    SenderUsername = sender?.Username ?? "unknown",
                    SenderDisplayName = sender == null ? "Unknown" : (string.IsNullOrWhiteSpace(sender.Name) ? sender.Username : sender.Name),
                    Message = message.Message,
                    SentAtUtc = message.SentAtUtc,
                    ReadAtUtc = message.ReadAtUtc,
                    IsMine = message.SenderUserId == currentUserId
                };
            }).ToList();
        }

        public async Task<DmMessageDTO?> SendDirectMessageAsync(int currentUserId, string otherUsername, string message)
        {
            var currentUser = await _dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Id == currentUserId);
            var otherUser = await _dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Username == otherUsername.Trim());

            if (currentUser == null || otherUser == null || currentUser.Id == otherUser.Id)
            {
                return null;
            }

            var thread = await GetOrCreateDirectMessageThreadAsync(currentUser.Id, otherUser.Id);

            var newMessage = new ChatMessageModel
            {
                ChatThreadId = thread.Id,
                SenderUserId = currentUser.Id,
                Message = message.Trim(),
                SentAtUtc = DateTime.UtcNow,
                ReadAtUtc = null
            };

            await _dataContext.ChatMessages.AddAsync(newMessage);
            await _dataContext.SaveChangesAsync();

            return new DmMessageDTO
            {
                Id = newMessage.Id,
                ChatThreadId = newMessage.ChatThreadId,
                SenderUserId = newMessage.SenderUserId,
                SenderUsername = currentUser.Username,
                SenderDisplayName = string.IsNullOrWhiteSpace(currentUser.Name) ? currentUser.Username : currentUser.Name,
                Message = newMessage.Message,
                SentAtUtc = newMessage.SentAtUtc,
                ReadAtUtc = newMessage.ReadAtUtc,
                IsMine = true
            };
        }

        public async Task<int> MarkDirectMessageConversationReadAsync(int currentUserId, string otherUsername)
        {
            var otherUser = await _dataContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Username == otherUsername.Trim());

            if (otherUser == null)
            {
                return 0;
            }

            var thread = await GetDirectMessageThreadAsync(currentUserId, otherUser.Id);
            if (thread == null)
            {
                return 0;
            }

            var unreadMessages = await _dataContext.ChatMessages
                .Where(message => message.ChatThreadId == thread.Id &&
                    message.SenderUserId != currentUserId &&
                    message.ReadAtUtc == null)
                .ToListAsync();

            if (unreadMessages.Count == 0)
            {
                return 0;
            }

            var readAtUtc = DateTime.UtcNow;
            foreach (var unreadMessage in unreadMessages)
            {
                unreadMessage.ReadAtUtc = readAtUtc;
            }

            await _dataContext.SaveChangesAsync();
            return unreadMessages.Count;
        }

        public async Task<List<ChatMessageDTO>?> GetChatMessagesAsync(int userId, int chatId)
        {
            var thread = await _dataContext.ChatThreads.SingleOrDefaultAsync(t => t.Id == chatId);
            if (thread == null || (thread.InitiatorUserId != userId && thread.RecipientUserId != userId))
            {
                return null;
            }

            var messages = await _dataContext.ChatMessages
                .Where(m => m.ChatThreadId == chatId)
                .OrderBy(m => m.SentAtUtc)
                .ToListAsync();

            var senderIds = messages.Select(m => m.SenderUserId).Distinct().ToList();
            var names = await _dataContext.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.Name) ? u.Username : u.Name);

            return messages.Select(m => new ChatMessageDTO
            {
                Id = m.Id,
                ChatThreadId = m.ChatThreadId,
                SenderUserId = m.SenderUserId,
                SenderName = names.GetValueOrDefault(m.SenderUserId, "Unknown"),
                Message = m.Message,
                SentAtUtc = m.SentAtUtc
            }).ToList();
        }

        public async Task<ChatMessageDTO?> SendChatMessageAsync(int userId, int chatId, SendChatMessageDTO input)
        {
            var thread = await _dataContext.ChatThreads.SingleOrDefaultAsync(t => t.Id == chatId);
            if (thread == null || thread.Status == "Closed")
            {
                return null;
            }

            if (thread.InitiatorUserId != userId && thread.RecipientUserId != userId)
            {
                return null;
            }

            var message = new ChatMessageModel
            {
                ChatThreadId = chatId,
                SenderUserId = userId,
                Message = input.Message.Trim(),
                SentAtUtc = DateTime.UtcNow
            };

            await _dataContext.ChatMessages.AddAsync(message);
            await _dataContext.SaveChangesAsync();

            var sender = await _dataContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
            return new ChatMessageDTO
            {
                Id = message.Id,
                ChatThreadId = message.ChatThreadId,
                SenderUserId = message.SenderUserId,
                SenderName = sender == null ? "Unknown" : (string.IsNullOrWhiteSpace(sender.Name) ? sender.Username : sender.Name),
                Message = message.Message,
                SentAtUtc = message.SentAtUtc
            };
        }

        public async Task<bool> EndChatAsync(int userId, int chatId)
        {
            var thread = await _dataContext.ChatThreads.SingleOrDefaultAsync(t => t.Id == chatId);
            if (thread == null)
            {
                return false;
            }

            if (thread.InitiatorUserId != userId && thread.RecipientUserId != userId)
            {
                return false;
            }

            if (thread.Status == "Closed")
            {
                return true;
            }

            thread.Status = "Closed";
            thread.EndedAtUtc = DateTime.UtcNow;
            await _dataContext.SaveChangesAsync();
            return true;
        }

        private static UserInfoDTO MapToUserInfo(UserModel user)
        {
            return new UserInfoDTO
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Description = user.Description
            };
        }

        private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371;

            var latDelta = DegreesToRadians(lat2 - lat1);
            var lonDelta = DegreesToRadians(lon2 - lon1);
            var lat1Rad = DegreesToRadians(lat1);
            var lat2Rad = DegreesToRadians(lat2);

            var a = Math.Sin(latDelta / 2) * Math.Sin(latDelta / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(lonDelta / 2) * Math.Sin(lonDelta / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round(earthRadiusKm * c, 2);
        }

        private async Task<ChatThreadDTO?> MapChatThreadAsync(ChatThreadModel thread)
        {
            var users = await _dataContext.Users
                .Where(u => u.Id == thread.InitiatorUserId || u.Id == thread.RecipientUserId)
                .ToDictionaryAsync(u => u.Id, u => string.IsNullOrWhiteSpace(u.Name) ? u.Username : u.Name);

            return new ChatThreadDTO
            {
                Id = thread.Id,
                HelpPostId = thread.HelpPostId,
                InitiatorUserId = thread.InitiatorUserId,
                RecipientUserId = thread.RecipientUserId,
                InitiatorName = users.GetValueOrDefault(thread.InitiatorUserId, "Unknown"),
                RecipientName = users.GetValueOrDefault(thread.RecipientUserId, "Unknown"),
                Status = thread.Status,
                StartedAtUtc = thread.StartedAtUtc,
                EndedAtUtc = thread.EndedAtUtc
            };
        }

        private async Task<ChatThreadModel?> GetDirectMessageThreadAsync(int currentUserId, int otherUserId)
        {
            return await _dataContext.ChatThreads
                .SingleOrDefaultAsync(thread => thread.HelpPostId == DirectMessageHelpPostId &&
                    ((thread.InitiatorUserId == currentUserId && thread.RecipientUserId == otherUserId) ||
                     (thread.InitiatorUserId == otherUserId && thread.RecipientUserId == currentUserId)));
        }

        private async Task<ChatThreadModel> GetOrCreateDirectMessageThreadAsync(int currentUserId, int otherUserId)
        {
            var thread = await GetDirectMessageThreadAsync(currentUserId, otherUserId);
            if (thread != null)
            {
                if (thread.Status == "Closed")
                {
                    thread.Status = "Active";
                    thread.EndedAtUtc = null;
                    await _dataContext.SaveChangesAsync();
                }

                return thread;
            }

            thread = new ChatThreadModel
            {
                HelpPostId = DirectMessageHelpPostId,
                InitiatorUserId = currentUserId,
                RecipientUserId = otherUserId,
                Status = "Active",
                StartedAtUtc = DateTime.UtcNow
            };

            await _dataContext.ChatThreads.AddAsync(thread);
            await _dataContext.SaveChangesAsync();
            return thread;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}