using CommunityToolkit.Mvvm.ComponentModel;

namespace ShinRyuModManager.UserInterface.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase {
    [ObservableProperty] private string _version;
    [ObservableProperty] private string _creditsText;

    public AboutWindowViewModel() {
        Initialize();
    }

    private void Initialize() {
        Version = $"v{Utils.GetAppVersion()}";

        using var credits = UiHelpers.LoadResourceAsStream("credits.txt");
        using var sr = new StreamReader(credits);

        CreditsText = sr.ReadToEnd();
    }
}
