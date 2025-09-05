using System.Collections.Generic;
using SteamAchievementCardManager.ViewModels;

namespace SteamAchievementCardManager.Models;

public class AppSettings
{
    public MainWindowViewModel.SortOrder LastSortOrder { get; set; } = MainWindowViewModel.SortOrder.AZ;
    public List<uint> RecentGameIds { get; set; } = new List<uint>();

}
