using Avalonia.Controls;
using Avalonia.Interactivity;
using ShinRyuModManager.UserInterface.ViewModels;
using Utils;

namespace ShinRyuModManager.UserInterface.UserControls;

public partial class LibraryDisplayControl : UserControl {
    private readonly LibMeta _meta;
    private LibMeta _localMeta;
    private bool _isLibraryInstalled;
    private bool _isLibraryEnabled;
    private bool _isLibraryUpdateAvailable;
    
    public LibraryDisplayControl() {
        InitializeComponent();
    }

    public LibraryDisplayControl(LibMeta meta) : this() {
        DataContext = new LibraryDisplayControlViewModel(meta);
        
        _meta = meta;

        CompareToLocalInstallation();
        UpdateButtonVisibility();
    }

    private void CompareToLocalInstallation() {
        if (DataContext is not LibraryDisplayControlViewModel viewModel) return;
        
        var dirPath = Path.Combine(GamePath.LIBRARIES, _meta.GUID.ToString());
        var metaPath = Path.Combine(dirPath, Settings.LIBRARIES_LIBMETA_FILE_NAME);

        if (Directory.Exists(dirPath)) {
            _isLibraryInstalled = true;

            var yamlString = File.ReadAllText(metaPath);
            
            _localMeta = LibMeta.ReadLibMeta(yamlString);
            _isLibraryEnabled = !File.Exists(Path.Combine(GamePath.LibrariesPath, _meta.GUID.ToString(), ".disabled"));
            _isLibraryUpdateAvailable = Utils.CompareVersionIsHigher(_meta.Version, _localMeta.Version);

            if (_isLibraryUpdateAvailable)
                viewModel.Version = $"{_meta.Version} (Installed: {_localMeta.Version})";
        } else {
            _isLibraryInstalled = false;
        }
    }

    private void UpdateButtonVisibility() {
        if (DataContext is not LibraryDisplayControlViewModel viewModel) return;
        
        if (_isLibraryInstalled) {
            viewModel.DisableBtnVisibility = true;

            if (_isLibraryUpdateAvailable) {
                viewModel.UpdateBtnVisibility = true;
            }

            if (_isLibraryEnabled) {
                if (_meta.CanBeDisabled)
                    viewModel.DisableBtnVisibility = true;
            } else {
                viewModel.EnableBtnVisibility = true;
            }
        } else {
            viewModel.InstallBtnVisibility = true;
        }

        if (!string.IsNullOrEmpty(_meta.Source) && _meta.Source.StartsWith("http")) {
            viewModel.SourceBtnVisibility = true;
            viewModel.SourceLink = _meta.Source;
        }
    }

    private void Source_OnClick(object sender, RoutedEventArgs e) {
        throw new NotImplementedException();
    }

    private void Install_OnClick(object sender, RoutedEventArgs e) {
        throw new NotImplementedException();
    }

    private void Uninstall_OnClick(object sender, RoutedEventArgs e) {
        throw new NotImplementedException();
    }

    private void Enable_OnClick(object sender, RoutedEventArgs e) {
        throw new NotImplementedException();
    }

    private void Disable_OnClick(object sender, RoutedEventArgs e) {
        throw new NotImplementedException();
    }

    private void Update_OnClick(object sender, RoutedEventArgs e) {
        throw new NotImplementedException();
    }
}

