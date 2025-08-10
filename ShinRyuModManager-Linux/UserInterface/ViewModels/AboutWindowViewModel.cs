namespace ShinRyuModManager.UserInterface.ViewModels;

public class AboutWindowViewModel : ViewModelBase {
    public string Version { get; private set; }
    public string CreditsText { get; private set; }

    public AboutWindowViewModel() {
        Initialize();
    }

    private void Initialize() {
        Version = $"v{Utils.GetAppVersion()}";

        using var credits = UIHelpers.LoadResourceAsStream("credits.txt");
        using var sr = new StreamReader(credits);

        CreditsText = sr.ReadToEnd();
    }
}
