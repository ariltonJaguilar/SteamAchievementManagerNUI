using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using SteamAchievementCardManager.Services;
using SteamAchievementCardManager.ViewModels;
using SteamAchievementCardManager.Views;
using Steamworks;

namespace SteamAchievementCardManager;

public partial class App : Application
{
    private SteamService? _steamService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            _steamService = new SteamService();

            if (!SteamAPI.Init())
            {
                throw new Exception("Falha ao inicializar o SteamAPI.");
            }

            var steamName = SteamFriends.GetPersonaName();
            Console.WriteLine($"Usuário logado na Steam: {steamName}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao conectar Steam: {e.Message}");
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            desktop.Exit += (_, _) =>
            {
                _steamService?.Shutdown();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
