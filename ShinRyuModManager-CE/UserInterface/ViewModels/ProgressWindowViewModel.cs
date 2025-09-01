namespace ShinRyuModManager.UserInterface.ViewModels;

public class ProgressWindowViewModel : ViewModelBase {
    public string MessageText { get; private set; }
    public bool IsIndeterminate { get; private set; }

    public ProgressWindowViewModel() { }

    public ProgressWindowViewModel(string messageText, bool isIndeterminate) {
        MessageText = messageText;
        IsIndeterminate = isIndeterminate;
    }
}
