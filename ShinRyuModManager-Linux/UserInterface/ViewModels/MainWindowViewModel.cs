using System.Collections.ObjectModel;

namespace ShinRyuModManager.UserInterface.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    public ObservableCollection<ModInfo> Mods { get; }

    public MainWindowViewModel() {
        var mods = new List<ModInfo> {
            new ModInfo("Test Mod 1", true),
            new ModInfo("Test Mod 2", true),
            new ModInfo("Test Mod 3", false)
        };

        Mods = new ObservableCollection<ModInfo>(mods);
    }
}

public class ModInfo() {
    public string ModName { get; set; }
    public bool Enabled { get; set; }

    public ModInfo(string modName, bool enabled) : this() {
        ModName = modName;
        Enabled = enabled;
    }
}