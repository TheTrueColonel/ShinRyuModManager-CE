using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ShinRyuModManager.UserInterface.ViewModels;

namespace ShinRyuModManager.UserInterface;

public class ViewLocator : IDataTemplate {
    public Control Build(object param) {
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null) {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBox {
            Text = $"Not Found: {name}"
        };
    }

    public bool Match(object data) {
        return data is ViewModelBase;
    }
}
