using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

public static class PreloadDatabase
{
    const string ResourcePath = "AChatFull.Resources.ChatDB.db"; // namespace + папка + имя

    public static async Task<string> GetDatabasePathAsync()
    {
        Debug.WriteLine("TESTLOG GetDatabasePathAsync");

        var localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var filePath = Path.Combine(localFolder, "ChatDB.db");

        if (!File.Exists(filePath))
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(ResourcePath))
            {
                if (stream == null)
                    throw new Exception($"Не найден ресурс {ResourcePath}");

                // И классический using для outStream:
                using (var outStream = File.Create(filePath))
                {
                    await stream.CopyToAsync(outStream);
                }
            }
        }

        return filePath;
    }
}