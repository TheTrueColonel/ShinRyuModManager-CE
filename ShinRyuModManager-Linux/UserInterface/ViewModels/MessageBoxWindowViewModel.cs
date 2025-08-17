namespace ShinRyuModManager.UserInterface.ViewModels;

public class MessageBoxWindowViewModel : ViewModelBase {
    public string MessageText { get; private set; }
    public bool IsVisible { get; private set; }

    public MessageBoxWindowViewModel() { }

    public MessageBoxWindowViewModel(string messageText, bool isVisible) {
        MessageText = messageText;
        IsVisible = isVisible;
    }
}
