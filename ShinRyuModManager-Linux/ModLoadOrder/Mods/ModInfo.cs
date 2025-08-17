using System.ComponentModel;
using System.Runtime.CompilerServices;
using Utils;

namespace ShinRyuModManager.ModLoadOrder.Mods;

public class ModInfo : IEqualityComparer<ModInfo>, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    
    private readonly string _name;
    private bool _enabled;
    
    public string Name {
        get => _name;
        
        private init {
            _name = value;
            NotifyPropertyChanged();
        }
    }
    
    public bool Enabled {
        get => _enabled;

        private set {
            _enabled = value;
            NotifyPropertyChanged();
        }
    }
    
    public ModInfo(string name, bool enabled = true) {
        Name = name;
        Enabled = enabled;
    }
    
    public void ToggleEnabled() {
        Enabled = !Enabled;
    }
    
    public bool Equals(ModInfo x, ModInfo y) {
        return x?.Name == y?.Name;
    }
    
    public int GetHashCode(ModInfo obj) {
        return obj.GetHashCode();
    }
    
    public static bool IsValid(ModInfo info) {
        return (info != null) && Directory.Exists(Path.Combine(GamePath.MODS, info.Name));
    }
    
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
