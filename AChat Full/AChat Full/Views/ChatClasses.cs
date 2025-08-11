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
        public string UserName { get; set; }
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

    [Table("Contacts")]
    public class Contact
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public string UserId { get; set; }
        [Indexed] public string ContactId { get; set; }
        public string DisplayName { get; set; }
        public long LastSeenUnix { get; set; } // Unix time
        public string AvatarUrl { get; set; }
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

        /// <summary>
        /// Убедиться, что таблица Contacts создана
        /// </summary>
        public Task EnsureContactsTableAsync()
        {
            return _db.CreateTableAsync<Contact>();
        }

        /// <summary>
        /// Добавляет или обновляет контакт текущего пользователя
        /// </summary>
        public async Task AddOrUpdateContactAsync(Contact c)
        {
            await EnsureContactsTableAsync();
            var existing = await _db.Table<Contact>()
                .Where(x => x.UserId == _currentUserId && x.ContactId == c.ContactId)
                .FirstOrDefaultAsync();
            if (existing == null)
            {
                c.UserId = _currentUserId;
                await _db.InsertAsync(c);
            }
            else
            {
                existing.DisplayName = c.DisplayName;
                existing.LastSeenUnix = c.LastSeenUnix;
                existing.AvatarUrl = c.AvatarUrl;
                await _db.UpdateAsync(existing);
            }
        }

        public async Task<List<Contact>> GetContactsAsync(string search = null, string sort = "name")
        {
            await EnsureContactsTableAsync();
            var q = _db.Table<Contact>().Where(x => x.UserId == _currentUserId);
            var list = await q.ToListAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                list = list.Where(x => (x.DisplayName ?? "").ToLowerInvariant().Contains(s)).ToList();
            }
            if (sort == "lastseen")
            {
                list = list.OrderByDescending(x => x.LastSeenUnix).ThenBy(x => x.DisplayName).ToList();
            }
            else
            {
                list = list.OrderBy(x => x.DisplayName).ThenByDescending(x => x.LastSeenUnix).ToList();
            }
            return list;
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
                    title = user?.UserName ?? other;
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
        public Task<List<Message>> GetMessagesForChatAsync(string chatId)
        {
            return _db.Table<Message>()
                      .Where(m => m.ChatId == chatId)
                      .OrderBy(m => m.CreatedAt)      // по возрастанию (старые вверху)
                      .ToListAsync();
        }

        /// <summary>
        /// Вставляет новое сообщение в таблицу Messages.
        /// Возвращает число затронутых строк (1 при успехе).
        /// </summary>
        public Task<int> InsertMessageAsync(Message message)
        {
            // Если вам нужно заменять существующее при совпадении PK:
            // return _db.InsertOrReplaceAsync(message);

            // Обычный Insert
            return _db.InsertAsync(message);
        }
    }
}
