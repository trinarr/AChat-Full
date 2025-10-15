using Android.Content;
using Xamarin.Forms;
using AChatFull.Services;

[assembly: Dependency(typeof(AChatFull.Droid.Services.SettingsService_Android))]
namespace AChatFull.Droid.Services
{
    public class SettingsService_Android : ISettingsService
    {
        const string FileName = "APP_SETTINGS";
        readonly ISharedPreferences _prefs;

        public SettingsService_Android()
        {
            var ctx = Android.App.Application.Context;
            _prefs = ctx.GetSharedPreferences(FileName, FileCreationMode.Private);
        }

        public int GetInt(string key, int defaultValue = 0)
            => _prefs.GetInt(key, defaultValue);

        public void SetInt(string key, int value)
        {
            using (var e = _prefs.Edit())
            {
                e.PutInt(key, value);
                e.Apply();
            }
        }

        public bool GetBool(string key, bool defaultValue = false)
            => _prefs.GetBoolean(key, defaultValue);

        public void SetBool(string key, bool value)
        {
            using (var e = _prefs.Edit())
            {
                e.PutBoolean(key, value);
                e.Apply();
            }
        }
    }
}
