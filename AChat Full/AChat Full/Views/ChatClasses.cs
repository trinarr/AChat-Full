using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using System.Diagnostics;
using System.Globalization;

namespace AChatFull.Views
{
    /// <summary>
    /// Представление одного сообщения в чате.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>Текст сообщения.</summary>
        public string Text { get; set; }

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
