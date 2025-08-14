using AChatFull.Views; // ваш enum Presence

namespace AChatFull.Utils
{
    public static class PresenceExtensions
    {
        /// <summary>Online | Idle | DoNotDisturb | Offline</summary>
        public static string ToLabel(this Presence p)
        {
            switch (p)
            {
                case Presence.Online: return "Online";
                case Presence.Idle: return "Idle";
                case Presence.DoNotDisturb: return "DoNotDisturb";
                default: return "Offline";
            }
        }

        /// <summary>Более «читаемо»: Do not disturb</summary>
        public static string ToReadableLabel(this Presence p)
        {
            switch (p)
            {
                case Presence.Online: return "Online";
                case Presence.Idle: return "Idle";
                case Presence.DoNotDisturb: return "Do not disturb";
                default: return "Offline";
            }
        }
    }
}