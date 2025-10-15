namespace AChatFull.Services
{
    public interface ISettingsService
    {
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);

        bool GetBool(string key, bool defaultValue = false);
        void SetBool(string key, bool value);
    }
}
