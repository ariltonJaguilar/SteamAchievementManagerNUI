using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamAchievementCardManager.Models;

public class GameInfo : INotifyPropertyChanged
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string CoverUrl { get; set; } = string.Empty;
    public string? FullCoverUrl { get; set; }
    private Bitmap? _fullCover;
    public Bitmap? FullCover
    {
        get => _fullCover;
        set
        {
            if (_fullCover == value)
                return;

            _fullCover = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullCover)));
        }
    }
    private Bitmap? _icon;
    public Bitmap? Icon
    {
        get => _icon;
        set
        {
            if (_icon == value)
                return;

            _icon = value;
            OnPropertyChanged();
        }
    }

    private Bitmap? _cover;
    public Bitmap? Cover
    {
        get => _cover;
        set
        {
            if (_cover == value)
                return;

            _cover = value;
            OnPropertyChanged();
        }
    }

    private bool _isImageLoading;
    public bool IsImageLoading
    {
        get => _isImageLoading;
        set
        {
            if (_isImageLoading == value)
                return;

            _isImageLoading = value;
            OnPropertyChanged();
        }
    }


    private int _totalAchievements;
    public int TotalAchievements
    {
        get => _totalAchievements;
        set
        {
            if (_totalAchievements == value)
                return;

            _totalAchievements = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AchievementsLabel));
        }
    }

    private int _unlockedAchievements;
    public int UnlockedAchievements
    {
        get => _unlockedAchievements;
        set
        {
            if (_unlockedAchievements == value)
                return;

            _unlockedAchievements = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AchievementsLabel));
        }
    }

    private bool _isAchievementsLoading;
    public bool IsAchievementsLoading
    {
        get => _isAchievementsLoading;
        set
        {
            if (_isAchievementsLoading == value)
                return;

            _isAchievementsLoading = value;
            OnPropertyChanged();
        }
    }

    public string AchievementsLabel =>
        TotalAchievements > 0 ? $"{UnlockedAchievements}/{TotalAchievements}" : string.Empty;

    public GameInfo(uint id, string name, string type = "normal")
    {
        Id = id;
        Name = name;
        Type = type;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}