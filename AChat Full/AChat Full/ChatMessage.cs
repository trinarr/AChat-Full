using System;

namespace AChatFull
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
}
