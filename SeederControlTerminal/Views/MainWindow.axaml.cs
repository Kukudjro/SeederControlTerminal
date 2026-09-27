using Avalonia.Controls;
using SeederControlTerminal.ViewModels;
using System.Collections.Specialized;


namespace SeederControlTerminal.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel viewModel)
        {
            // автоскролл
            viewModel.LogEntries.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems?.Count > 0)
                {

                    var actualItem = args.NewItems[0];
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var logListBox = this.FindControl<ListBox>("LogListBox");

                        if (logListBox != null && viewModel.LogEntries.Count > 0)
                        {
                            int lastIndex = viewModel.LogEntries.Count - 1;

                            logListBox.ScrollIntoView(lastIndex);
                        }
                    });
                }
            };
        } 
    }

}