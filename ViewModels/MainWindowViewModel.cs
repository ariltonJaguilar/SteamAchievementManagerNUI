using Avalonia.Media.Imaging;
using SteamAchievementCardManager.Services;
using SAM.API;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml.XPath;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SteamAchievementCardManager.Models;

namespace SteamAchievementCardManager.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly SteamService _steamService;
    private readonly SteamGameService _steamGameService;
    private readonly AppSettings _settings;

    private Client? _samClient;

    public string SteamUserName { get; private set; }
    public Bitmap? SteamAvatar { get; private set; }
    public string SteamId { get; private set; }
    public string SteamStatus { get; private set; }
    public int SteamLevel { get; private set; }
    public string SteamLanguage { get; private set; }
    public string? CurrentGame { get; private set; }
    public int FriendsCount { get; private set; }

    // Lista completa de jogos
    public ObservableCollection<GameInfo> Games { get; } = new();

    // Lista filtrada (usada pelo ListBox)
    private ObservableCollection<GameInfo> _filteredGames = new();
    public ObservableCollection<GameInfo> FilteredGames
    {
        get => _filteredGames;
        private set
        {
            _filteredGames = value;
            OnPropertyChanged(nameof(FilteredGames));
        }
    }

    // Texto da pesquisa
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                UpdateFilteredGames();
            }
        }
    }

    // Ordem de ordenação
    public enum SortOrder
    {
        Native,
        AZ,
        ZA
    }

    private SortOrder _currentSortOrder = SortOrder.AZ;
    public SortOrder CurrentSortOrder
    {
        get => _currentSortOrder;
        private set
        {
            if (_currentSortOrder != value)
            {
                _currentSortOrder = value;
                OnPropertyChanged(nameof(CurrentSortOrder));
                UpdateFilteredGames();
            }
        }
    }

    public ICommand SortCommand { get; }
    private GameInfo? _selectedGame;
    public GameInfo? SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (SetProperty(ref _selectedGame, value) && value != null)
            {
                GameClickedCommand.Execute(value);
            }
        }
    }
    private async Task LoadRecentGameCovers()
    {
        foreach (var game in RecentGames)
        {
            if (game.FullCover == null && !string.IsNullOrEmpty(game.FullCoverUrl))
            {
                try
                {
                    using var webClient = new WebClient();
                    var data = await webClient.DownloadDataTaskAsync(game.FullCoverUrl);
                    using var ms = new MemoryStream(data);
                    game.FullCover = Bitmap.DecodeToWidth(ms, 96);
                }
                catch
                {
                    game.FullCover = null;
                }
            }
        }
    }
    public ICommand RefreshGamesCommand { get; }

    public MainWindowViewModel()
    {
        // Inicializa Steam
        _steamService = new SteamService();
        SteamUserName = _steamService.GetUserName();
        SteamAvatar = _steamService.GetUserAvatar();
        SteamId = _steamService.GetSteamId().ToString();
        SteamStatus = _steamService.GetPersonaStateString();
        SteamLevel = _steamService.GetSteamLevel();
        // Carrega configurações salvas
        _settings = SettingsService.Load();
        _isGridView = _settings.IsGridView;

        _currentSortOrder = _settings.LastSortOrder;
        // Comando de ordenação
        RecentGames.CollectionChanged += (_, __) => OnPropertyChanged(nameof(RecentGames));

        SortCommand = new DelegateCommand<string>(param =>
        {
            switch (param?.ToUpperInvariant())
            {
                case "AZ":
                    CurrentSortOrder = SortOrder.AZ;
                    break;
                case "ZA":
                    CurrentSortOrder = SortOrder.ZA;
                    break;
                case "NATIVE":
                default:
                    CurrentSortOrder = SortOrder.Native;
                    break;
            }

            // Salva sempre que mudar a ordenação
            _settings.LastSortOrder = CurrentSortOrder;
            SettingsService.Save(_settings);
        });

        // Inicializa SAM
        InitSAM();
        
        GameClickedCommand = new DelegateCommand<GameInfo>(async game =>
        {
            if (game == null) return;

            // Remove se já estiver na lista
            if (RecentGames.Contains(game))
                RecentGames.Remove(game);

            // Adiciona no início
            RecentGames.Insert(0, game);

            // Limita a 5 itens
            while (RecentGames.Count > 5)
                RecentGames.RemoveAt(5);

            // Atualiza AppSettings
            _settings.RecentGameIds = RecentGames.Select(g => g.Id).ToList();
            SettingsService.Save(_settings);

            // Baixa a capa completa em tempo real
            if (game.FullCover == null)
            {
                try
                {
                    using var webClient = new WebClient();
                    var data = await webClient.DownloadDataTaskAsync(game.FullCoverUrl);
                    using var ms = new MemoryStream(data);
                    game.FullCover = Bitmap.DecodeToWidth(ms, 96);
                }
                catch
                {
                    game.FullCover = null;
                }
            }
        });
        RefreshGamesCommand = new DelegateCommand<object>(async _ =>
        {
            IsLoading = true; // começa o loading

            try
            {
                Games.Clear();
                FilteredGames.Clear();

                await Task.Run(() => LoadGames());

                LoadRecentGamesFromSettings();
            }
            finally
            {
                IsLoading = false; // termina o loading
            }
        });


    }

    private void InitSAM()
    {
        try
        {
            _samClient = new Client();
            _samClient.Initialize(0);

            // Carrega a lista de jogos
            LoadGames();
        }
        catch (ClientInitializeException e)
        {
            ErrorMessage = "Steam is not running. Please open Steam and try again.";

           // ErrorMessage = "Steam não está rodando.\nAbra o Steam e tente novamente.\n\n" + e.Message;
        }
        catch (DllNotFoundException)
        {
            ErrorMessage = "DLL not found";
        }
        catch (Exception e)
        {
            ErrorMessage = "Unknow error: " + e.Message;
        }
    }
// Lista de últimos 5 jogos clicados
    private ObservableCollection<GameInfo> _recentGames = new();
    public ObservableCollection<GameInfo> RecentGames
    {
        get => _recentGames;
        private set
        {
            _recentGames = value;
            OnPropertyChanged(nameof(RecentGames));
        }
    }
    private bool _isGridView;
    public bool IsGridView
    {
        get => _isGridView;
        set
        {
            if (_isGridView != value)
            {
                _isGridView = value;
                OnPropertyChanged();
            }
        }
    }
    public ICommand ToggleLayoutCommand => new RelayCommand(() =>
    {
        IsGridView = !IsGridView;

        // Salva no settings
        _settings.IsGridView = IsGridView;
        SettingsService.Save(_settings);
    });

// Comando quando um jogo for clicado
    public ICommand GameClickedCommand { get; }
    private void LoadGames()
    {
        if (_samClient == null)
            return;

        List<KeyValuePair<uint, string>> allGames = new();

        try
        {
            // Baixa a lista oficial de jogos SAM
            using var client = new System.Net.WebClient();
            byte[] bytes = client.DownloadData("https://gib.me/sam/games.xml");

            using var stream = new MemoryStream(bytes, false);
            var doc = new System.Xml.XPath.XPathDocument(stream);
            var nav = doc.CreateNavigator();
            var nodes = nav.Select("/games/game");
            while (nodes.MoveNext())
            {
                string type = nodes.Current.GetAttribute("type", "");
                if (string.IsNullOrEmpty(type)) type = "normal";

                allGames.Add(new KeyValuePair<uint, string>((uint)nodes.Current.ValueAsLong, type));
            }
        }
        catch (Exception e)
        {
            ErrorMessage = "Unable to download the game list.";

           // ErrorMessage = "Não foi possível baixar a lista de jogos SAM.\n" + e.Message;
            return;
        }

        // Filtra apenas jogos que o usuário possui
        foreach (var kv in allGames)
        {
            uint appId = kv.Key;
            if (_samClient.SteamApps008.IsSubscribedApp(appId))
            {
                string name = _samClient.SteamApps001.GetAppData(appId, "name") ?? $"Game {appId}";
                var game = new GameInfo(appId, name, kv.Value);

                // Tenta obter e carregar o ícone do jogo
                try
                {
                    var iconHash = _samClient.SteamApps001.GetAppData(appId, "icon");
                    if (!string.IsNullOrWhiteSpace(iconHash))
                    {
                        var url = $"https://media.steampowered.com/steamcommunity/public/images/apps/{appId}/{iconHash}.jpg";
                    }
                }
                catch
                {
                    // Se falhar, deixa sem ícone
                }

                // Define apenas a URL da capa
               // game.CoverUrl = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appId}/capsule_184x69.jpg";
               game.CoverUrl =  $"https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/{appId}/header.jpg";
                // Capa completa para recentes
                game.FullCoverUrl = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appId}/library_600x900_2x.jpg";
                Games.Add(game);
            }
        }

        // Inicializa lista filtrada com todos os jogos
        UpdateFilteredGames();
        // Carrega recentes do AppSettings
        LoadRecentGamesFromSettings();
        _ = LoadRecentGameCovers();

    }
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }
    }
    private void LoadRecentGamesFromSettings()
    {
        if (_settings.RecentGameIds == null || _settings.RecentGameIds.Count == 0)
            return;

        // Só adiciona jogos que ainda não estão na lista
        foreach (var id in _settings.RecentGameIds)
        {
            if (RecentGames.Any(g => g.Id == id))
                continue;

            var game = Games.FirstOrDefault(g => g.Id == id);
            if (game != null)
            {
                RecentGames.Add(game);
            }
        }
    }

    private void UpdateFilteredGames()
    {
        IEnumerable<GameInfo> source;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            source = Games;
        }
        else
        {
            source = Games.Where(g => g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Aplica ordenação
        switch (CurrentSortOrder)
        {
            case SortOrder.AZ:
                source = source.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase);
                break;
            case SortOrder.ZA:
                source = source.OrderByDescending(g => g.Name, StringComparer.CurrentCultureIgnoreCase);
                break;
            case SortOrder.Native:
            default:
                // Mantém a ordem original
                break;
        }

        FilteredGames = new ObservableCollection<GameInfo>(source);
    }

    // Mensagem de erro para mostrar no conteúdo principal
    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
            HasError = !string.IsNullOrWhiteSpace(value);

            if (HasError)
                StartErrorTimer();
        }
    }
    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set
        {
            if (_hasError != value)
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }
    }
    private async void StartErrorTimer()
    {
        await Task.Delay(4000); // fica visível 4 segundos
        HasError = false;
    }

    // Comando auxiliar simples
    private sealed class DelegateCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public DelegateCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => _execute((T?)parameter);
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
