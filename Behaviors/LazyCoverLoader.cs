using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SteamAchievementCardManager.Models;

namespace SteamAchievementCardManager.Behaviors
{
    public static class LazyCoverLoader
    {
        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(LazyCoverLoader));

        private static readonly HttpClient Http = new HttpClient();
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(4); // limite de concorrência

        static LazyCoverLoader()
        {
            IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
        }

        public static void SetIsEnabled(AvaloniaObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(AvaloniaObject element) => element.GetValue(IsEnabledProperty);

        private static void OnIsEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != IsEnabledProperty)
                return;

            var enabled = e.GetNewValue<bool>();

            if (enabled)
            {
                control.AttachedToVisualTree += Control_AttachedToVisualTree;
                control.DetachedFromVisualTree += Control_DetachedFromVisualTree;
            }
            else
            {
                control.AttachedToVisualTree -= Control_AttachedToVisualTree;
                control.DetachedFromVisualTree -= Control_DetachedFromVisualTree;
                control.PropertyChanged -= Control_PropertyChanged_IsVisible;
            }
        }

        private static void Control_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Control control)
            {
                control.PropertyChanged -= Control_PropertyChanged_IsVisible;
            }
        }

        private static void Control_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is not Control control)
                return;
            control.PropertyChanged -= Control_PropertyChanged_IsVisible;
            control.PropertyChanged += Control_PropertyChanged_IsVisible;
            if (control.IsVisible)
            {
                _ = TryLoadAsync(control);
            }
        }

        private static async void Control_PropertyChanged_IsVisible(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            try
            {
                if (sender is Control control &&
                    e.Property == Visual.IsVisibleProperty &&
                    control.IsVisible)
                {
                    await TryLoadAsync(control);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Control_PropertyChanged_IsVisible: {ex}");
            }
        }

        private static async Task TryLoadAsync(Control control)
        {
            if (control.DataContext is not GameInfo game)
                return;
            if (game.Cover is not null || game.IsImageLoading)
                return;
            if (string.IsNullOrWhiteSpace(game.CoverUrl))
                return;

            game.IsImageLoading = true;
            int maxRetries = 3;
            int delayMs = 500;

            try
            {
                await Gate.WaitAsync().ConfigureAwait(false);

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var bytes = await Http.GetByteArrayAsync(game.CoverUrl).ConfigureAwait(false);
                        using var ms = new MemoryStream(bytes);
                        var bmp = new Bitmap(ms);

                        await Dispatcher.UIThread.InvokeAsync(() => game.Cover = bmp);
                        return; // sucesso, sai do método
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fail to load async (attempt {attempt}/{maxRetries}) for URL: {game.CoverUrl}");
                        Console.WriteLine($"Error: {ex.Message}");

                        if (attempt < maxRetries)
                            await Task.Delay(delayMs); // espera antes da próxima tentativa
                    }
                }
            }
            finally
            {
                game.IsImageLoading = false;
                try
                {
                    Gate.Release();
                }
                catch
                {
                    Console.WriteLine($"Fail to release semaphore for {game.Name}");
                }
            }
        }
    }
}