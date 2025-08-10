using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ShinRyuModManager.ModLoadOrder.Mods;
using Utils;

namespace ShinRyuModManager.UserInterface.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    public string TitleText { get; private set; } = "Shin Ryu Mod Manager";
    public string AppVersionText { get; private set; } = "SRMM Version";

    [ObservableProperty] private string _modName = "Mod Name";
    [ObservableProperty] private string _modDescription = "Mod Description";
    [ObservableProperty] private string _modAuthor = "Author";
    [ObservableProperty] private string _modVersion = "Version";
    [ObservableProperty] private ObservableCollection<ModInfo> _modList;

    public MainWindowViewModel() {
        Initialize();
        LoadModList();
    }

    public void SelectMod(ModMeta mod) {
        ModName = mod.Name;
        ModDescription = mod.Description;
        ModAuthor = mod.Author;
        ModVersion = mod.Version;
    }

    private void Initialize() {
        TitleText = $"Shin Ryu Mod Manager [{GamePath.GetGameFriendlyName(GamePath.CurrentGame)}]";
        AppVersionText = $"v{Utils.GetAppVersion()}";

        Directory.CreateDirectory(GamePath.MODS);
        Directory.CreateDirectory(GamePath.LIBRARIES);
    }

    internal void LoadModList() {
        ModList = new ObservableCollection<ModInfo>(Program.PreRun());
    }
}