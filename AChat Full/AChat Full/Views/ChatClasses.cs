using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using System.Diagnostics;
using System.Globalization;

namespace AChatFull.Views
{
    public enum MessageKind { Text = 0, Document = 1 }

    /// <summary>
    /// Представление одного сообщения в чате.
    /// </summary>
    public class ChatMessage
    {
        public MessageKind Kind { get; set; } = MessageKind.Text;
        public string Text { get; set; }

        public DocumentInfo Document { get; set; }    // если Kind=Document

        /// <summary>Флаг: входящее (true) или исходящее (false).</summary>
        public bool IsIncoming { get; set; }

        /// <summary>Метка времени (необязательно).</summary>
        public DateTime Timestamp { get; set; }
    }
    public class ChatSummary
    {
        [PrimaryKey]
        public string ChatId { get; set; }
        public string Title { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastTimestamp { get; set; }
        public bool IsRead { get; set; }
    }

    /// <summary>
    /// Таблица чатов (Chats)
    /// </summary>
    [Table("Chats")]
    public class Chat
    {
        [PrimaryKey]
        public string ChatId { get; set; }
    }

    // Модель таблицы Users
    [Table("Users")]
    public class User
    {
        [PrimaryKey]
        public string UserId { get; set; }

        // 1 = ещё нет чата (показывать в списке Контактов), 0 = чат уже был
        public int IsContact { get; set; } = 0;

        // Профиль
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BirthDate { get; set; } 
        public string About { get; set; }   
        public string Status { get; set; }
        public string AvatarUrl { get; set; }

        [Ignore]
        public DateTime BirthDateDate
            => DateTime.ParseExact(
                 BirthDate,
                 "yyyy-MM-dd HH:mm:ss",
                 CultureInfo.InvariantCulture);

        public string DisplayName =>
           string.IsNullOrWhiteSpace(FirstName)
               ? (string.IsNullOrWhiteSpace(LastName) ? UserId : LastName)
               : (string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}");

        public string DisplayStatus => Status;
        public bool HasStatus => !string.IsNullOrWhiteSpace(Status);

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
        public bool NoAvatar => string.IsNullOrWhiteSpace(AvatarUrl);

        public string Initials
        {
            get
            {
                var f = (FirstName ?? "").Trim();
                var l = (LastName ?? "").Trim();
                var fi = string.IsNullOrEmpty(f) ? "" : f.Substring(0, 1).ToUpperInvariant();
                var li = string.IsNullOrEmpty(l) ? "" : l.Substring(0, 1).ToUpperInvariant();
                var res = fi + li;
                if (!string.IsNullOrEmpty(res)) return res;

                var dn = (DisplayName ?? "").Trim();
                return string.IsNullOrEmpty(dn) ? "#" : dn.Substring(0, Math.Min(2, dn.Length)).ToUpperInvariant();
            }
        }
    }

    // Модель таблицы ChatParticipants
    [Table("ChatParticipants")]
    public class ChatParticipant
    {
        [Indexed]
        public string ChatId { get; set; }

        [Indexed]
        public string UserId { get; set; }
    }

    /// <summary>
    /// Таблица сообщений (Messages)
    /// </summary>
    [Table("Messages")]
    public class Message
    {
        [PrimaryKey, AutoIncrement]
        public int MessageId { get; set; }
        [Indexed]
        public string ChatId { get; set; }
        public string SenderId { get; set; }
        public string Text { get; set; }
        public string CreatedAt { get; set; }
        //public bool IsRead { get; set; }

        // NEW:
        public int Type { get; set; }                 // 0=Text, 1=Document
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }
        public string RemoteUrl { get; set; }
        public string LocalPath { get; set; }

        [Ignore]
        public DateTime CreatedAtDate
        => DateTime.ParseExact(
             CreatedAt,
             "yyyy-MM-dd HH:mm:ss",
             CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Репозиторий для работы с SQLite-базой чатов
    /// </summary>
    public class ChatRepository
    {
        private readonly SQLiteAsyncConnection _db;
        private readonly string _currentUserId;

        public ChatRepository(string dbPath, string currentUserId)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _currentUserId = currentUserId;
        }

        public async Task<User> GetCurrentUserProfileAsync() =>
    await _db.Table<User>().Where(u => u.UserId == _currentUserId).FirstOrDefaultAsync();

        public async Task SaveCurrentUserProfileAsync(User profile)
        {
            profile.UserId = _currentUserId;
            await _db.InsertOrReplaceAsync(profile);
        }

        public Task<List<User>> SearchUsersAsync(string query, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _db.Table<User>()
                          .OrderBy(u => u.FirstName)
                          .Take(limit)
                          .ToListAsync();

            // Упрощённая экранизация для LIKE
            string safe = query.Replace("%", "").Replace("_", "");
            string like = "%" + safe + "%";

            const string sql = @"
        SELECT * FROM Users
        WHERE FirstName LIKE ?
           OR LastName  LIKE ?
           OR UserId    LIKE ?
        ORDER BY IsContact DESC, FirstName
        LIMIT ?";

            return _db.QueryAsync<User>(sql, like, like, like, limit);
        }

        public Task<User> GetUserAsync(string userId)
        {
            return _db.Table<User>()
                      .Where(u => u.UserId == userId)
                      .FirstOrDefaultAsync();
        }

        public Task<Chat> GetChatAsync(string chatId)
        {
            return _db.Table<Chat>()
                      .Where(c => c.ChatId == chatId)
                      .FirstOrDefaultAsync();
        }

        public Task<List<User>> GetContactsAsync(int limit = 500)
        {
            return _db.Table<User>()
                      .Where(u => u.IsContact == 1)
                      .OrderBy(u => u.FirstName)
                      .Take(limit)
                      .ToListAsync();
        }

        public Task MarkUserAsContactAsync(string otherUserId)
        {
            return _db.ExecuteAsync("UPDATE Users SET IsContact = 1 WHERE UserId = ?", otherUserId);
        }

        /// <summary>
        /// Возвращает UserId собеседника для заданного чата из таблицы ChatParticipants:
        /// выбираем любую строку с таким ChatId, где UserId <> мой.
        /// </summary>
        public async Task<string> GetPeerUserIdFromParticipantsAsync(string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(App.USER_TOKEN_TEST))
                return null;

            const string sql = "SELECT UserId FROM ChatParticipants WHERE ChatId = ? AND UserId <> ? LIMIT 1";
            try
            {
                return await _db.ExecuteScalarAsync<string>(sql, chatId, App.USER_TOKEN_TEST);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetOrCreateDirectChatIdAsync(string otherUserId)
        {
            // все чаты, где участвует текущий пользователь
            var myParts = await _db.Table<ChatParticipant>()
                                   .Where(p => p.UserId == _currentUserId)
                                   .ToListAsync();

            // перебираем чаты и ищем тот, где второй участник = otherUserId
            foreach (var chatId in myParts.Select(p => p.ChatId).Distinct())
            {
                var other = await _db.Table<ChatParticipant>()
                                     .Where(p => p.ChatId == chatId && p.UserId == otherUserId)
                                     .FirstOrDefaultAsync();

                if (other != null)
                    return chatId; // нашли существующий 1:1
            }

            // 2) Чата нет — создаём новый
            /*var newChatId = Guid.NewGuid().ToString();

            var chat = new Chat
            {
                ChatId = newChatId,
                IsDirect = 1,
                CreatedAt = DateTime.UtcNow
            };
            await _db.InsertAsync(chat);

            await _db.InsertAsync(new ChatParticipant { ChatId = newChatId, UserId = _currentUserId });
            await _db.InsertAsync(new ChatParticipant { ChatId = newChatId, UserId = otherUserId });

            return newChatId;*/

            return null;
        }

        /// <summary>
        /// Возвращает список ChatSummary, сгруппированный по ChatId,
        /// с информацией о последнем сообщении и его статусе.
        /// </summary>
        public async Task<List<ChatSummary>> GetChatSummariesAsync()
        {
            Debug.WriteLine("TESTLOG GetChatSummariesAsync");

            // Получаем все чаты
            var chats = await _db.Table<Chat>().ToListAsync();
            var result = new List<ChatSummary>(chats.Count);

            foreach (var chat in chats)
            {
                // 1) берём последнее сообщение
                var last = await _db.Table<Message>()
                                    .Where(m => m.ChatId == chat.ChatId)
                                    .OrderByDescending(m => m.CreatedAt)
                                    .FirstOrDefaultAsync();

                if (last == null) continue;

                var participants = await _db.Table<ChatParticipant>()
                                            .Where(p => p.ChatId == chat.ChatId)
                                            .ToListAsync();

                var other = participants
                .Select(p => p.UserId)
                .FirstOrDefault(uid => uid != _currentUserId);
                string title;
                if (other != null)
                {
                    var user = await _db.Table<User>()
                                        .Where(u => u.UserId == other)
                                        .FirstOrDefaultAsync();
                    title = user?.FirstName ?? other;
                }
                else
                {
                    // однопользовательский чат? fallback на ChatId
                    title = chat.ChatId;
                }

                result.Add(new ChatSummary
                {
                    ChatId = chat.ChatId,
                    Title = title,
                    LastMessage = last.Text,
                    LastTimestamp = last.CreatedAtDate,
                    //IsRead = last.IsRead
                });
            }

            Debug.WriteLine("TESTLOG GetChatSummariesAsync result "+ result.Count);

            // сортируем по дате последнего сообщения (сначала свежие)
            return result
                   .OrderByDescending(x => x.LastTimestamp)
                   .ToList();
        }

        // Новый метод:

        // New overload: get last {take} messages, optionally older than 'beforeExclusiveCreatedAt' (format "yyyy-MM-dd HH:mm:ss")
        public Task<List<Message>> GetMessagesForChatAsync(string chatId, int take, string beforeExclusiveCreatedAt)
        {
            if (string.IsNullOrEmpty(beforeExclusiveCreatedAt))
            {
                // Latest first
                return _db.Table<Message>()
                          .Where(m => m.ChatId == chatId)
                          .OrderByDescending(m => m.CreatedAt)
                          .Take(take)
                          .ToListAsync();
            }
            else
            {
                // Use raw SQL for robust string comparison in SQLite
                const string sql = "SELECT * FROM Message WHERE ChatId = ? AND CreatedAt < ? ORDER BY CreatedAt DESC LIMIT ?";
                return _db.QueryAsync<Message>(sql, chatId, beforeExclusiveCreatedAt, take);
            }
        }
        public Task<int> InsertMessageAsync(Message message)
        {
            // Если вам нужно заменять существующее при совпадении PK:
            // return _db.InsertOrReplaceAsync(message);

            // Обычный Insert
            return _db.InsertAsync(message);
        }
    }
}
