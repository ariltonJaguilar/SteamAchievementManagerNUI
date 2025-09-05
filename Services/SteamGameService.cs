using System.Collections.Generic;
using SteamAchievementCardManager.Models;

namespace SteamAchievementCardManager.Services;

public class SteamGameService
{
    private readonly SAM.API.Client _steamClient;

    public SteamGameService(SAM.API.Client client)
    {
        _steamClient = client;
    }

    public List<GameInfo> GetOwnedGames(List<KeyValuePair<uint, string>> allGames)
    {
        var ownedGames = new List<GameInfo>();

        foreach (var kv in allGames)
        {
            uint appId = kv.Key;
            string type = kv.Value;

            if (_steamClient.SteamApps008.IsSubscribedApp(appId))
            {
                string name = _steamClient.SteamApps001.GetAppData(appId, "name") ?? $"Game {appId}";
                var gameInfo = new GameInfo(appId, type) { Name = name };
                ownedGames.Add(gameInfo);
            }
        }

        return ownedGames;
    }
}
