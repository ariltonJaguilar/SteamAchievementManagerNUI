using SteamAchievementCardManager.ViewModels;

namespace SteamAchievementCardManager.Models;

public class AppSettings
{
    public MainWindowViewModel.SortOrder LastSortOrder { get; set; } = MainWindowViewModel.SortOrder.AZ;
}
