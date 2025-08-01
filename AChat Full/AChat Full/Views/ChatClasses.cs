using System;

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
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    public class ChatSummary
    {
        public string ChatId { get; set; }
        public string Title { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastTimestamp { get; set; }
        public int UnreadCount { get; set; }
    }
}
