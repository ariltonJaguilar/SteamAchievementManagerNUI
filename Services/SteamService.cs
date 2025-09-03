using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Platform;
using Steamworks;
using System;
using System.IO;
using SAM.API;

namespace SteamAchievementCardManager.Services;

public class SteamService
{
    private bool _initialized;
    private Client _samClient;

    public SteamService()
    {
        try
        {
            _initialized = SteamAPI.Init();
        }
        catch
        {
            _initialized = false;
        }
        InitSAM();
    }
    private void InitSAM()
    {
        _samClient = new Client();
        _samClient.Initialize(0); // 0 = pega AppId do Steam em execução
    }
    public string GetUserName()
    {
        if (!_initialized) return "Não conectado";
        return SteamFriends.GetPersonaName();
    }

    public Bitmap? GetUserAvatar()
    {
        if (!_initialized) return null;

        int avatar = SteamFriends.GetLargeFriendAvatar(SteamUser.GetSteamID());
        if (avatar == -1) return null; // ainda não carregou

        if (SteamUtils.GetImageSize(avatar, out uint width, out uint height))
        {
            var buffer = new byte[4 * width * height];
            if (SteamUtils.GetImageRGBA(avatar, buffer, buffer.Length))
            {
                // Cria um WriteableBitmap no tamanho correto
                var bmp = new WriteableBitmap(new PixelSize((int)width, (int)height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);

                using (var fb = bmp.Lock())
                {
                    // Copia pixels RGBA
                    System.Runtime.InteropServices.Marshal.Copy(buffer, 0, fb.Address, buffer.Length);
                }

                return bmp;
            }
        }
        return null;
    }

    public void Shutdown()
    {
        if (_initialized)
            SteamAPI.Shutdown();
    }
    public bool OwnsGame(uint appId)
    {
        return _samClient.SteamApps008.IsSubscribedApp(appId);
    }

    // alterna o contexto para um AppID específico
    public void EnterAppContext(uint appId)
    {
        _samClient.Initialize(appId);
    }

    //  volta para o contexto "neutro"
    public void ExitAppContext()
    {
        _samClient.Initialize(0);
    }

    // acesso ao SAM Client para chamadas diretas quando preciso
    public Client SamClient => _samClient;
}