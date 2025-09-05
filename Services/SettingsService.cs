using System;
using System.IO;
using System.Text.Json;
using SteamAchievementCardManager.Models;

namespace SteamAchievementCardManager.Services
{
    public static class SettingsService
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SAM",              
            "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // Se der erro, retorna configurações padrão
            }

            return new AppSettings();
        }

        // Salva as configurações no disco
        public static void Save(AppSettings settings)
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir!))
                    Directory.CreateDirectory(dir!);

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Aqui você pode logar o erro se quiser
            }
        }
    }
}