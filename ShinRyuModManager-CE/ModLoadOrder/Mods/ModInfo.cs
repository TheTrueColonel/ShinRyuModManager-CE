using CommunityToolkit.Mvvm.ComponentModel;
using Utils;

namespace ShinRyuModManager.ModLoadOrder.Mods;

public sealed partial class ModInfo : ObservableObject, IEquatable<ModInfo> {
    [ObservableProperty] private string _name;
    [ObservableProperty] private bool _enabled;

    public ModInfo(string name, bool enabled = true) {
        Name = name;
        Enabled = enabled;
    }

    public void ToggleEnabled() {
        Enabled = !Enabled;
    }
    
    public static bool IsValid(ModInfo info) {
        return (info != null) && Directory.Exists(Path.Combine(GamePath.MODS, info.Name));
    }

    public bool Equals(ModInfo other) {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Name == other.Name;
    }

    public override bool Equals(object obj) {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((ModInfo)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Name, Enabled);
    }
}
