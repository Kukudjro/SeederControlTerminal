using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SeederControlTerminal.Services.Log;
using SeederControlTerminal.ViewModels;
using SeederControlTerminal.Views;

namespace SeederControlTerminal;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {

            ILogService logService = new LogService();

            Services.IMessageSender tcpSender = new Services.TcpJsonMessageSender();
            var viewModel = new MainWindowViewModel(tcpSender, logService);

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}