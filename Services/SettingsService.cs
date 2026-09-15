using System;
using System.IO;
using System.Text.Json;

namespace MinecraftTracker.Services
{
    // Modelo simples das preferências do usuário.
    public class AppSettings
    {
        // "System" (segue o Windows) | "Light" | "Dark"
        public string AppTheme { get; set; } = "System";
        public bool PlayStartupSound { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
    }

    // Persiste as configurações em um settings.json ao lado do executável.
    // Como o projeto é "unpackaged" (WindowsPackageType=None), evitamos
    // Windows.Storage.ApplicationData, que exige identidade de pacote MSIX.
    public static class SettingsService
    {
        private static readonly string SettingsPath =
            Path.Combine(AppContext.BaseDirectory, "settings.json");

        private static AppSettings? _current;
        public static AppSettings Current => _current ??= Load();

        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
            }
            catch
            {
                // Se o arquivo estiver corrompido ou ilegível, cai para o padrão
                // em vez de derrubar o app.
            }

            return new AppSettings();
        }

        public static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Falha ao salvar não deve interromper o uso do app.
            }
        }
    }
}
