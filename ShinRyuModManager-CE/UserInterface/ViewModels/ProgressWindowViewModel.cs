using CommunityToolkit.Mvvm.ComponentModel;

namespace ShinRyuModManager.UserInterface.ViewModels;

public partial class ProgressWindowViewModel : ViewModelBase {
    [ObservableProperty] private string _messageText;
    [ObservableProperty] private bool _isIndeterminate;

    public ProgressWindowViewModel() { }

    public ProgressWindowViewModel(string messageText, bool isIndeterminate) {
        MessageText = messageText;
        IsIndeterminate = isIndeterminate;
    }
}
