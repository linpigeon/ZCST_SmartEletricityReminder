using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WaterElectricityAutoClient.ViewModels;
using WaterElectricityAutoClient.Views;

namespace WaterElectricityAutoClient;

public partial class MainWindow : Window
{
    private NativeMenu? _trayMenu;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isExiting) return;

        e.Cancel = true;

        var dialog = new CloseDialog();
        await dialog.ShowDialog(this);

        if (dialog.Choice == CloseDialog.CloseChoice.Background)
        {
            Hide();
        }
        else
        {
            _isExiting = true;
            Close();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        TryCreateTrayIcon();
    }

    private void TryCreateTrayIcon()
    {
        try
        {
            var icon = CreateSimpleIcon();
            if (icon == null) return;

            var tray = new TrayIcon
            {
                Icon = icon,
                ToolTipText = "完美校园水电查询系统",
                Menu = new NativeMenu()
            };

            var showItem = new NativeMenuItem("显示窗口");
            showItem.Click += (_, _) => ShowWindow();
            tray.Menu.Add(showItem);

            tray.Menu.Add(new NativeMenuItemSeparator());

            var exitItem = new NativeMenuItem("退出程序");
            exitItem.Click += (_, _) => ExitApp();
            tray.Menu.Add(exitItem);

            tray.Clicked += (_, _) => ShowWindow();

            _trayMenu = tray.Menu;
        }
        catch
        {
            // System tray not supported on this platform
        }
    }

    private static WindowIcon? CreateSimpleIcon()
    {
        try
        {
            // Create a 32x32 blue "电量" icon using SkiaSharp
            var skBitmap = new SkiaSharp.SKBitmap(32, 32);
            using var canvas = new SkiaSharp.SKCanvas(skBitmap);
            canvas.Clear(new SkiaSharp.SKColor(0x00, 0x78, 0xD4));

            using var paint = new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.White,
                IsAntialias = true
            };
            using var font = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.Default, 20);
            canvas.DrawText("⚡", 16, 22, SkiaSharp.SKTextAlign.Center, font, paint);

            var image = SkiaSharp.SKImage.FromBitmap(skBitmap);
            var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);

            return new WindowIcon(new MemoryStream(data.ToArray()));
        }
        catch
        {
            return null;
        }
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        _isExiting = true;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
        else
        {
            Close();
        }
    }
}
