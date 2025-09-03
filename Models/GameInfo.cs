using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class GameInfo : INotifyPropertyChanged
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "normal"; // normal, demo, mod, junk
    public string CoverUrl { get; set; } = string.Empty;

    private Bitmap? _icon;
    public Bitmap? Icon
    {
        get => _icon;
        set
        {
            if (_icon != value)
            {
                _icon = value;
                OnPropertyChanged();
            }
        }
    }

    private Bitmap? _cover;
    public Bitmap? Cover
    {
        get => _cover;
        set
        {
            if (_cover != value)
            {
                _cover = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isImageLoading;
    public bool IsImageLoading
    {
        get => _isImageLoading;
        set
        {
            if (_isImageLoading != value)
            {
                _isImageLoading = value;
                OnPropertyChanged();
            }
        }
    }

    private int _totalAchievements;
    public int TotalAchievements
    {
        get => _totalAchievements;
        set
        {
            if (_totalAchievements != value)
            {
                _totalAchievements = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AchievementsLabel));
            }
        }
    }

    private int _unlockedAchievements;
    public int UnlockedAchievements
    {
        get => _unlockedAchievements;
        set
        {
            if (_unlockedAchievements != value)
            {
                _unlockedAchievements = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AchievementsLabel));
            }
        }
    }

    private bool _isAchievementsLoading;
    public bool IsAchievementsLoading
    {
        get => _isAchievementsLoading;
        set
        {
            if (_isAchievementsLoading != value)
            {
                _isAchievementsLoading = value;
                OnPropertyChanged();
            }
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
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}