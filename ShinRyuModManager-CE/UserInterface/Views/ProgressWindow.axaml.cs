using Avalonia.Controls;
using ShinRyuModManager.UserInterface.ViewModels;

namespace ShinRyuModManager.UserInterface.Views;

public partial class ProgressWindow : Window {
    // ReSharper disable once MemberCanBePrivate.Global
    public ProgressWindow() { 
        InitializeComponent();
    }
    
    public ProgressWindow(string text, bool isIndeterminate) : this() {
        DataContext = new ProgressWindowViewModel(text, isIndeterminate);
    }
    
    public static async Task<bool> Show(Window owner, string message, bool isIndeterminate) {
        var win = new ProgressWindow(message, isIndeterminate) {
            Title = "Please wait...",
            Icon = owner.Icon
        };
        
        return await win.ShowDialog<bool>(owner);
    }
}

