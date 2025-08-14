using AChatFull.Views; 

namespace AChatFull.Utils
{
    public static class PresenceExtensions
    {
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