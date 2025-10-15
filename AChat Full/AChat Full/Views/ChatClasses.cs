using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using AChatFull.ViewModels;

namespace AChatFull.Views
{
    public enum MessageKind { Text = 0, Document = 1 }
    public enum Presence
    {
        Offline,
        Online,
        Idle,
        Invisible,
        DoNotDisturb
    }
    public enum ClientType
    {
        ICQ,
        ICQ2,
        QIP,
        INFIUM,
        AIM,
        JIMM,
        MIRANDA,
        MIRC,
        RQ
    }

    public class CustomStatusModel
    {
        public string Emoji { get; set; }
        public string Text { get; set; }
    }

    public class ChatMessage
    {
        public MessageKind Kind { get; set; } = MessageKind.Text;
        public string Text { get; set; }

        public DocumentInfo Document { get; set; }    // если Kind=Document

        public bool IsIncoming { get; set; }

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

        public User Peer { get; set; }

        public string PeerAvatarUrl => Peer?.AvatarUrl;
        public bool PeerHasAvatar => Peer?.HasAvatar ?? false;
        public bool PeerNoAvatar => !PeerHasAvatar;
        public string PeerInitials => Peer?.Initials;
    }

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

        public int IsContact { get; set; } = 0;

        public string FirstName { get; set; }
        public string LastName { get; set; }
        [Column("Birthdate")]
        public string Birthdate { get; set; } // ХРАНИМ в виде "dd.MM.yyyy"
        [Column("About")]
        public string About { get; set; }
        public string StatusCustom { get; set; }
        public string AvatarUrl { get; set; }

        [Column("StatusDefault")]
        public Presence Presence { get; set; } = Presence.Offline;

        [Column("ClientType")]
        public ClientType ClientType { get; set; } = ClientType.ICQ;

        [Ignore]
        public DateTime BirthDateDate
            => DateTime.ParseExact(
                 Birthdate,
                 "yyyy-MM-dd HH:mm:ss",
                 CultureInfo.InvariantCulture);

        public string DisplayName =>
           string.IsNullOrWhiteSpace(FirstName)
               ? (string.IsNullOrWhiteSpace(LastName) ? UserId : LastName)
               : (string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}");

        public string DisplayStatus => StatusCustom;
        public bool HasStatus => !string.IsNullOrWhiteSpace(StatusCustom);
        public bool IsOnline => Presence != Presence.Offline && Presence != Presence.Invisible;

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

        public async Task DeleteChatAsync(string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId)) return;

            // 1) Соберём локальные файлы документов, чтобы удалить их с диска
            try
            {
                var docs = await _db.Table<Message>()
                                    .Where(m => m.ChatId == chatId && !string.IsNullOrEmpty(m.LocalPath))
                                    .ToListAsync();

                foreach (var d in docs)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(d.LocalPath) && File.Exists(d.LocalPath))
                            File.Delete(d.LocalPath);
                    }
                    catch
                    {
                        // Игнорируем ошибки IO, чтобы не ломать транзакцию БД
                    }
                }
            }
            catch
            {
                // Игнорируем — удаление из БД всё равно продолжим
            }

            // 2) Удаляем из БД в транзакции
            await _db.RunInTransactionAsync(conn =>
            {
                conn.Execute("DELETE FROM Messages WHERE ChatId = ?", chatId);
                conn.Execute("DELETE FROM ChatParticipants WHERE ChatId = ?", chatId);
                conn.Execute("DELETE FROM Chats WHERE ChatId = ?", chatId);
            });
        }

        public async Task UpdateProfileAsync(ProfileUpdate update)
        {
            if (update == null) return;

            var first = (update.FirstName ?? string.Empty).Trim();
            var last = (update.LastName ?? string.Empty).Trim();
            var about = (update.About ?? string.Empty).Trim();

            // Users.BirthDate у вас строкой, парсится BirthDateDate через "yyyy-MM-dd HH:mm:ss"
            string birthStr = null;
            if (update.Birthdate.HasValue && update.Birthdate.Value.Year > 1900)
                birthStr = update.Birthdate.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            // Обновляем существующую запись
            var rows = await _db.ExecuteAsync(
                "UPDATE Users SET FirstName = ?, LastName = ?, About = ?, BirthDate = ? WHERE UserId = ?",
                first, last, about, birthStr, _currentUserId);

            // Если текущего пользователя ещё нет — создаём
            if (rows == 0)
            {
                await _db.InsertAsync(new User
                {
                    UserId = _currentUserId,
                    FirstName = first,
                    LastName = last,
                    About = about,
                    Birthdate = birthStr
                });
            }

            // Дадим знать UI, что профиль обновился
            Xamarin.Forms.MessagingCenter.Send<object>(this, "ProfileChanged");
        }

        public async Task<string> UpdateAvatarAsync(Stream imageStream, string fileName)
        {
            if (imageStream == null) return null;

            // Папка для аватаров в памяти приложения
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "avatars");
            Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

            var path = Path.Combine(dir, $"{_currentUserId}{ext}");

            // Сохраняем файл
            using (var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await imageStream.CopyToAsync(fs);
            }

            // Обновляем ссылку в БД
            var rows = await _db.ExecuteAsync(
                "UPDATE Users SET AvatarUrl = ? WHERE UserId = ?",
                path, _currentUserId);

            if (rows == 0)
                await _db.InsertAsync(new User { UserId = _currentUserId, AvatarUrl = path });

            Xamarin.Forms.MessagingCenter.Send<object>(this, "ProfileChanged");
            return path;
        }

        public async Task UpdatePresenceAsync(string presence)
        {
            var p = NormalizePresence(presence);

            // Пытаемся обновить запись текущего пользователя.
            var rows = await _db.ExecuteAsync(
                "UPDATE Users SET StatusDefault = ? WHERE UserId = ?",
                (int)p, _currentUserId);

            // Если пользователя ещё нет в таблице — создадим.
            if (rows == 0)
                await _db.InsertAsync(new User { UserId = _currentUserId, Presence = p });
        }

        public async Task UpdateCustomStatusAsync(CustomStatusModel model)
        {
            var status = ComposeCustomStatus(model);

            var rows = await _db.ExecuteAsync(
                "UPDATE Users SET StatusCustom = ? WHERE UserId = ?",
                status, _currentUserId);

            if (rows == 0)
                await _db.InsertAsync(new User { UserId = _currentUserId, StatusCustom = status });
        }

        // ---------- приватные помощники ----------
        private static Presence NormalizePresence(string presence)
        {
            if (string.IsNullOrWhiteSpace(presence)) return Presence.Offline;
            var p = presence.Trim().ToLowerInvariant();

            switch (p)
            {
                case "online":
                    return Presence.Online;
                case "away":
                case "idle":
                    return Presence.Idle;
                case "busy":
                case "dnd":
                case "do not disturb":
                case "do_not_disturb":
                case "do-not-disturb":
                case "donotdisturb":
                    return Presence.DoNotDisturb;
                case "offline":
                case "invisible":
                default:
                    return Presence.Offline;
            }
        }

        private static string ComposeCustomStatus(CustomStatusModel model)
        {
            if (model == null) return null;

            // Храним короткую строку: "😊 Working on PR"
            var emoji = string.IsNullOrWhiteSpace(model.Emoji) ? "" : model.Emoji.Trim();
            var text = string.IsNullOrWhiteSpace(model.Text) ? "" : model.Text.Trim();

            var combined = (emoji + " " + text).Trim();
            return string.IsNullOrEmpty(combined) ? null : combined;
        }

        /// <summary>
        /// Возвращает кастомный статус текущего пользователя,
        /// парся Users.StatusCustom (пример: "😊 На встрече").
        /// Если статус пуст — возвращает объект с null-полями.
        /// </summary>
        public async Task<CustomStatusModel> GetCustomStatusAsync()
        {
            // Подтянем текущего пользователя
            var u = await _db.Table<User>() // <-- замените тип, если у вас класс сущности называется иначе
                             .Where(x => x.UserId == _currentUserId)
                             .FirstOrDefaultAsync();

            if (u == null || string.IsNullOrWhiteSpace(u.StatusCustom))
                return new CustomStatusModel();

            var raw = u.StatusCustom.Trim();

            // Попытка отделить первый графемный кластер как "эмодзи",
            // иначе считаем, что весь статус — это текст без эмодзи.
            var si = new StringInfo(raw);
            var emoji = string.Empty;
            var text = raw;

            if (si.LengthInTextElements > 0)
            {
                var first = si.SubstringByTextElements(0, 1); // 1 графемный кластер
                var rest = raw.Substring(first.Length).TrimStart();

                // Эвристика: если первый кластер не буква/цифра, считаем его эмодзи
                var c0 = first[0];
                var isAlphaNum = char.IsLetterOrDigit(c0);

                if (!isAlphaNum || first.Length > 1) // surrogate/комбинированный — скорее всего эмодзи
                {
                    emoji = first;
                    text = rest;
                }
            }

            return new CustomStatusModel
            {
                Emoji = string.IsNullOrWhiteSpace(emoji) ? null : emoji,
                Text = string.IsNullOrWhiteSpace(text) ? null : text
            };
        }

        public Task ClearCustomStatusAsync()
        {
            // Если записи нет — UPDATE просто вернёт 0, это ок.
            return _db.ExecuteAsync(
                "UPDATE Users SET StatusCustom = NULL WHERE UserId = ?",
                _currentUserId);
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
            var newChatId = Guid.NewGuid().ToString();

            var chat = new Chat
            {
                ChatId = newChatId,
                //CreatedAt = DateTime.UtcNow
            };
            await _db.InsertAsync(chat);
            await _db.InsertAsync(new ChatParticipant { ChatId = newChatId, UserId = _currentUserId });
            await _db.InsertAsync(new ChatParticipant { ChatId = newChatId, UserId = otherUserId });
            await MarkUserAsContactAsync(otherUserId);

            return newChatId;
        }

        // 1. Снять флажок контакта
        public Task UnmarkUserAsContactAsync(string otherUserId)
            => _db.ExecuteAsync("UPDATE Users SET IsContact = 0 WHERE UserId = ?", otherUserId);

        // 2. Найти существующий direct-чат (НЕ создавая новый)
        public async Task<string> FindDirectChatIdAsync(string otherUserId)
        {
            if (string.IsNullOrWhiteSpace(otherUserId)) return null;

            var myParts = await _db.Table<ChatParticipant>()
                                   .Where(p => p.UserId == _currentUserId)
                                   .ToListAsync();

            foreach (var chatId in myParts.Select(p => p.ChatId).Distinct())
            {
                var other = await _db.Table<ChatParticipant>()
                                     .Where(p => p.ChatId == chatId && p.UserId == otherUserId)
                                     .FirstOrDefaultAsync();
                if (other != null) return chatId;
            }
            return null;
        }

        // 3. Проверка существования чата (для страховки в ChatPage)
        public async Task<bool> ChatExistsAsync(string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId)) return false;
            var chat = await _db.Table<Chat>().Where(c => c.ChatId == chatId).FirstOrDefaultAsync();
            return chat != null;
        }

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
                User user = null;
                if (other != null)
                {
                    user = await _db.Table<User>()
                                        .Where(u => u.UserId == other)
                                        .FirstOrDefaultAsync();
                    title = user?.DisplayName ?? other;
                }
                else
                {
                    title = chat.ChatId;
                }

                result.Add(new ChatSummary
                {
                    ChatId = chat.ChatId,
                    Title = title,
                    LastMessage = last.Text,
                    LastTimestamp = last.CreatedAtDate,
                    Peer = user
                    //IsRead = last.IsRead
                });
            }

            Debug.WriteLine("TESTLOG GetChatSummariesAsync result "+ result.Count);

            // сортируем по дате последнего сообщения (сначала свежие)
            return result
                   .OrderByDescending(x => x.LastTimestamp)
                   .ToList();
        }

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
