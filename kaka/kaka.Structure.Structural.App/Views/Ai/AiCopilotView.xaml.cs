#nullable enable
using System.Collections.Specialized;
using System.Windows.Input;
using MahApps.Metro.Controls;
using kaka.Structure.Structural.App.ViewModels.Ai;

namespace kaka.Structure.Structural.App.Views.Ai
{
  public partial class AiCopilotView : MetroWindow
  {
    public AiCopilotView()
    {
      InitializeComponent();
      Loaded += AiCopilotView_Loaded;
    }

    private void AiCopilotView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
      if (DataContext is AiCopilotViewModel vm)
      {
        vm.Messages.CollectionChanged += Messages_CollectionChanged;
      }
      TxtPromptInput.Focus();
    }

    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
      ChatScrollViewer?.ScrollToBottom();
    }

    private void TxtPromptInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        if (DataContext is AiCopilotViewModel vm)
        {
          vm.SendMessageCommand.Execute(null);
        }
      }
    }

    public void ExecutePrompt(string prompt)
    {
      if (DataContext is AiCopilotViewModel vm)
      {
        vm.ProcessPrompt(prompt);
      }
    }
  }
}
