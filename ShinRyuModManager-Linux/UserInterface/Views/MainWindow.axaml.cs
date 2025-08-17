using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ShinRyuModManager.Helpers;
using ShinRyuModManager.ModLoadOrder.Mods;
using ShinRyuModManager.UserInterface.ViewModels;
using Utils;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using Constants = Utils.Constants;

namespace ShinRyuModManager.UserInterface.Views;

// TODO: Add ability to edit ModMeta
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
    }

    // TODO: Add changelog auto popup
    private void Window_OnLoaded(object sender, RoutedEventArgs e) {
        RunPreInitAsync().ConfigureAwait(false);

        Program.ReadCachedLocalLibraryData();
        
        RefreshModList();
    }

    private async Task RunPreInitAsync() {
        // Referencing `Program` all the time isn't ideal. Maybe move to global settings/helper file?
        if (Program.ShouldBeExternalOnly()) {
            _ = await MessageBoxWindow.Show(this, "Warning", "External mods folder detected. Please run Shin Ryu Mod Manager in CLI mode (use --cli parameter) and use the external mod manager instead.");
        }

        if (ConsoleOutput.ShowWarnings) {
            if (Program.MissingDll()) {
                _ = await MessageBoxWindow.Show(this, "Warning", $"{Constants.DINPUT8DLL} is missing from this directory. Mods will NOT be applied without this file.");
            }

            if (Program.MissingAsi()) {
                _ = await MessageBoxWindow.Show(this, "Warning", $"{Constants.ASI} is missing from this directory. Mods will NOT be applied without this file.");
            }

            if (Program.InvalidGameExe()) {
                _ = await MessageBoxWindow.Show(this, "Error", $"Game version is unrecognized. Please use the latest Steam version of the game. The mod list will still be saved.\nMods may still work depending on the version.");
            }
        }
    }

    // Event Handlers
    private async void ModList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        try {
            var grid = sender as DataGrid;
            var selected = grid?.SelectedItem as ModInfo;

            if (selected == null) {
                return;
            }

            await UpdateModMetaAsync(DataContext as MainWindowViewModel, selected);
        } catch (Exception ex) {
            _ = await MessageBoxWindow.Show(this, "Error", $"An error has occurred. \nThe exception message is:\n\n{ex.Message}");
        }
    }

    private void ModToggle_OnClick(object sender, RoutedEventArgs e) {
        foreach (ModInfo mod in ModListView.SelectedItems) {
            mod.ToggleEnabled();
        }
    }

    private void ModUp_OnClick(object sender, RoutedEventArgs e) {
        var viewModel = DataContext as MainWindowViewModel;
        var selection = ModListView.SelectedItems.Cast<ModInfo>().OrderBy(m => viewModel!.ModList.IndexOf(m)).ToList();

        if (viewModel == null) return;

        var limit = 0;
        foreach (var i in selection.Select(m => viewModel.ModList.IndexOf(m)).OrderBy(x => x)) {
            if (i > limit) {
                (viewModel.ModList[i - 1], viewModel.ModList[i]) = (viewModel.ModList[i], viewModel.ModList[i - 1]);
            } else {
                ++limit;
            }
        }
        
        // Restore selection
        foreach (var mod in selection) {
            ModListView.SelectedItems.Add(mod);
        }
    }

    private void ModDown_OnClick(object sender, RoutedEventArgs e) {
        if (DataContext is not MainWindowViewModel viewModel) return;
        
        var selection = ModListView.SelectedItems.Cast<ModInfo>().OrderBy(m => viewModel.ModList.IndexOf(m)).ToList();

        var limit = viewModel.ModList.Count - 1;
        foreach (var i in selection.Select(m => viewModel.ModList.IndexOf(m)).OrderByDescending(x => x)) {
            if (i < limit) {
                (viewModel.ModList[i + 1], viewModel.ModList[i]) = (viewModel.ModList[i], viewModel.ModList[i + 1]);
            } else {
                --limit;
            }
        }
        
        // Restore selection
        foreach (var mod in selection) {
            ModListView.SelectedItems.Add(mod);
        }
    }

    private async void ModSave_OnClick(object sender, RoutedEventArgs e) {
        try {
            if (DataContext is not MainWindowViewModel viewModel) return;

            if (await Program.SaveModListAsync(viewModel.ModList.ToList())) {
                await CheckModDependenciesAsync();
                
                // Run generation only if it will not be run on game launch (i.e. if RebuildMlo is disabled or unsupported)
                // TODO: Figure out how RebuildMLO is automatically running
                if (Program.RebuildMlo && Program.IsRebuildMloSupported) {
                    _ = await MessageBoxWindow.Show(this, "Information", "Mod list was saved. Mods will be applied next time the game is run.");
                } else {
                    var progressWindow = new ProgressWindow("Applying mods. Please wait...", true);
                    
                    progressWindow.Show(this);

                    bool success;

                    try {
                        await Program.RunGeneration(Program.ConvertNewToOldModList(viewModel.ModList.ToList()));
                        success = true;
                    } catch {
                        success = false;
                    }
                    
                    progressWindow.Close();

                    if (!success) {
                        _ = await MessageBoxWindow.Show(this, "Error", "Mods could not be applied. Please make sure that the game directory has write access. " +
                            "\n\nRun Shin Ryu Mod Manager in command line mode (use --cli parameter) for more info.");
                    }
                }
            } else {
                _ = await MessageBoxWindow.Show(this, "Error", "Mod list is empty and was not saved.");
            }
        } catch (Exception ex) {
            _ = await MessageBoxWindow.Show(this, "Fatal", $"An error has occurred. \nThe exception message is:\n\n{ex.Message}");
        }
    }

    private async Task CheckModDependenciesAsync() {
        var modList = Program.ReadModListTxt(Constants.TXT);

        var modsWithDependencyProblems = new List<string>();
        var disabledLibraries = new List<string>();
        var missingLibraries = new List<string>();

        foreach (var enabledMod in modList.Where(x => x.Enabled)) {
            foreach (var dependencyGuid in Program.GetModDependencies(enabledMod.Name)) {
                if (!Program.DoesLibraryExist(dependencyGuid)) {
                    if(!modsWithDependencyProblems.Contains(enabledMod.Name))
                        modsWithDependencyProblems.Add(enabledMod.Name);

                    missingLibraries.Add(dependencyGuid);
                } else if (!Program.IsLibraryEnabled(dependencyGuid)) {
                    if (!modsWithDependencyProblems.Contains(enabledMod.Name))
                        modsWithDependencyProblems.Add(enabledMod.Name);

                    disabledLibraries.Add(dependencyGuid);
                }
            }
        }

        if (disabledLibraries.Count > 0 || missingLibraries.Count > 0) {
            var sb = new StringBuilder();

            if (missingLibraries.Count > 0) {
                sb.AppendLine("Missing libraries:");

                sb.AppendJoin('\n', missingLibraries.Select(Program.GetLibraryName));

                sb.AppendLine();
            }

            if (disabledLibraries.Count > 0) {
                sb.AppendLine("Disabled libraries:");

                sb.AppendJoin('\n', disabledLibraries.Select(Program.GetLibraryName));

                sb.AppendLine();
            }

            sb.AppendLine("The following mods depend on these libraries:");

            sb.AppendJoin('\n', modsWithDependencyProblems.Select(Program.GetLibraryName));

            sb.AppendLine();

            sb.Append("Your mods may not properly work without them. Consider installing or enabling them from the Libraries tab.");
            
            _ = await MessageBoxWindow.Show(this, "Mod Library Dependency Warning", sb.ToString());
        }
    }

    private void ModClose_OnClick(object sender, RoutedEventArgs e) => Close();

    private async void ModInstall_OnClick(object sender, RoutedEventArgs e) {
        try {
            Directory.CreateDirectory(GamePath.MODS);

            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                AllowMultiple = true,
                FileTypeFilter = [CustomFilePickerFileTypes.CompressedZip, CustomFilePickerFileTypes.CompressedRar, CustomFilePickerFileTypes.CompressedGzip, FilePickerFileTypes.All]
            });

            if (files.Count == 0)
                return;

            foreach (var file in files) {
                if (!File.Exists(file.TryGetLocalPath()))
                    return;

                if (await Utils.TryInstallModZipAsync(file.TryGetLocalPath()))
                    RefreshModList();
            }
                
        } catch (Exception ex) {
            _ = await MessageBoxWindow.Show(this, "Fatal", $"An error has occurred. \nThe exception message is:\n\n{ex.Message}");
        }
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e) {
        var window = new LibraryManagerWindow();
        window.Show(this);
    }

    private void ModListViewRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshModList();

    private async void ModMetaSampleYaml_OnClick(object sender, RoutedEventArgs e) {
        try {
            var saveDialog = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                SuggestedFileName = "mod-meta",
                DefaultExtension = ".yaml",
                ShowOverwritePrompt = true,
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(GamePath.ModsPath),
                FileTypeChoices = [CustomFilePickerFileTypes.YamlFile, FilePickerFileTypes.All]
            });

            if (saveDialog?.TryGetLocalPath() == null)
                return;

            var sampleMeta = new ModMeta {
                Name = "Your mod name",
                Author = "Author name",
                Version = "1.0.0",
                Description = "Mod description example.\nThis is in a new line.\n\nYou can use single (') or double (\") quotes in your text.\n\nIf you want your text to be more organized in the yaml file, make sure the new line is indented (press TAB)."
            };

            var serializer = new SerializerBuilder().WithDefaultScalarStyle(ScalarStyle.Plain).Build();
            var yaml = serializer.Serialize(sampleMeta);

            await File.WriteAllTextAsync(saveDialog.TryGetLocalPath()!, yaml, Encoding.UTF8);
        } catch (Exception ex) {
            _ = await MessageBoxWindow.Show(this, "Fatal", $"An error has occurred. \nThe exception message is:\n\n{ex.Message}");
        }
    }

    private async void ModMetaSampleImage_OnClick(object sender, RoutedEventArgs e) {
        try {
            var saveDialog = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
                SuggestedFileName = "mod-image",
                DefaultExtension = ".png",
                ShowOverwritePrompt = true,
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(GamePath.ModsPath),
                FileTypeChoices = [FilePickerFileTypes.ImagePng]
            });

            if (saveDialog?.TryGetLocalPath() == null)
                return;

            var sampleImage = await UIHelpers.LoadResourceAsBitmapAsync("NoImage.png");
            sampleImage.Save(saveDialog.TryGetLocalPath()!);
        } catch (Exception ex) {
            _ = await MessageBoxWindow.Show(this, "Fatal", $"An error has occurred. \nThe exception message is:\n\n{ex.Message}");
        }
    }

    private void About_OnClick(object sender, RoutedEventArgs e) {
        var window = new AboutWindow();
        window.Show(this);
    }

    private void ChangeLog_OnClick(object sender, RoutedEventArgs e) {
        var window = new ChangeLogWindow();
        window.Show(this);
    }

    // Private methods
    private async Task UpdateModMetaAsync(MainWindowViewModel viewModel, ModInfo mod) {
        const string modImagePattern = "mod-image.*";
        
        var modPath = Path.Combine(GamePath.ModsPath, mod.Name);
        var modMetaPath = Path.Combine(modPath, "mod-meta.yaml");
        //var libMetaPath = Path.Combine(modPath, "lib-meta.yaml");

        var matchingModImages = Directory.EnumerateFiles(modPath, modImagePattern);

        Bitmap modImage = null;

        try {
            var meta = ModMeta.GetPlaceholder(mod.Name);

            if (File.Exists(modMetaPath)) {
                meta = await YamlHelpers.DeserializeYamlFromPathAsync<ModMeta>(modMetaPath);
            }

            viewModel.SelectMod(meta);
        } catch (Exception ex) {
            _ = await MessageBoxWindow.Show(this, "Error", $"An error has occurred while trying to load mod-meta. \nThe exception message is:\n\n{ex.Message}");
        }

        foreach (var filePath in matchingModImages) {
            try {
                await using var fs = File.OpenRead(filePath);

                modImage = new Bitmap(fs);
                
                break;
            } catch (Exception ex) {
                _ = await MessageBoxWindow.Show(this, "Error", $"An error has occurred while trying to load {Path.GetFileName(filePath)}. \nThe exception message is:\n\n{ex.Message}");
            }
        }

        modImage ??= await UIHelpers.LoadResourceAsBitmapAsync("NoImage.png");

        ModImage.Source = modImage;
    }

    private void RefreshModList() {
        if (DataContext is not MainWindowViewModel viewModel) return;
        
        viewModel.LoadModList();
    }
}

