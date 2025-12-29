using System.Collections.ObjectModel;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ShinRyuModManager.Extensions;
using ShinRyuModManager.ModLoadOrder.Mods;
using Utils;

namespace ShinRyuModManager.UserInterface.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    [ObservableProperty] private string _titleText = "Shin Ryu Mod Manager";
    [ObservableProperty] private string _appVersionText = "SRMM Version";
    [ObservableProperty] private string _gameLaunchPath;
    
    [ObservableProperty] private string _modName = "Mod Name";
    [ObservableProperty] private string _modDescription = "Mod Description";
    [ObservableProperty] private string _modAuthor = "Author";
    [ObservableProperty] private string _modVersion = "Version";
    [ObservableProperty] private ObservableCollection<ModInfo> _modList;

    public MainWindowViewModel() {
        Initialize();
    }

    public void SelectMod(ModMeta mod) {
        ModName = mod.Name;
        ModDescription = mod.Description;
        ModAuthor = mod.Author;
        ModVersion = mod.Version;
    }
    
    [RelayCommand]
    public void UpdateProfile(Profile profile) {
        Program.ActiveProfile = profile;
        
        Log.Information("Setting Profile to ");
        
        LoadModList(profile);
    }

    private void Initialize() {
        AppVersionText = $"v{AssemblyVersion.GetVersion()}";

        // Prefer launching through Steam, but if Windows, allow launching via exe
        if (GamePath.IsSteamInstalled()) {
            GameLaunchPath = $"steam://launch/{GamePath.GetGameSteamId(GamePath.CurrentGame)}";
        } else if (OperatingSystem.IsWindows()) {
            GameLaunchPath = GamePath.GameExe;
        }

        Directory.CreateDirectory(GamePath.MODS);
        Directory.CreateDirectory(GamePath.LIBRARIES);
    }

    internal void LoadModList(Profile? profile = null) {
        ModList = new ObservableCollection<ModInfo>(Program.PreRun(profile));
        
        TitleText = $"Shin Ryu Mod Manager [{GamePath.GetGameFriendlyName(GamePath.CurrentGame)}] [{Program.ActiveProfile.GetDescription()}]";
    }
}