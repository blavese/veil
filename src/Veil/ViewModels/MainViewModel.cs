using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Veil.Services;

namespace Veil.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ProfileStore _store = new();
    private readonly WireGuardService _wireGuard = new();
    private readonly FirewallService _firewall = new();
    private readonly IpInfoService _ipInfo = new();
    private readonly DispatcherTimer _statusTimer;

    public ObservableCollection<ProfileViewModel> Profiles { get; } = new();

    private ProfileViewModel? _selected;
    public ProfileViewModel? Selected
    {
        get => _selected;
        set => Set(ref _selected, value);
    }

    private bool _killSwitchEnabled;
    public bool KillSwitchEnabled
    {
        get => _killSwitchEnabled;
        set
        {
            if (Set(ref _killSwitchEnabled, value))
                _ = ToggleKillSwitch(value);
        }
    }

    private string _publicIp = "-";
    public string PublicIp
    {
        get => _publicIp;
        set => Set(ref _publicIp, value);
    }

    private string _connectedSince = "";
    public string ConnectedSince
    {
        get => _connectedSince;
        set => Set(ref _connectedSince, value);
    }

    private string? _statusMessage;
    public string? StatusMessage
    {
        get => _statusMessage;
        set => Set(ref _statusMessage, value);
    }

    private DateTime? _connectStartedAt;

    public RelayCommand ImportCommand { get; }
    public RelayCommand ConnectToggleCommand { get; }
    public RelayCommand RemoveCommand { get; }

    public MainViewModel()
    {
        ImportCommand = new RelayCommand(ImportAsync);
        ConnectToggleCommand = new RelayCommand(ToggleConnectionAsync, () => Selected != null);
        RemoveCommand = new RelayCommand(RemoveSelectedAsync, () => Selected != null && !Selected.IsConnected);

        foreach (var profile in _store.Load())
        {
            var vm = new ProfileViewModel(profile);
            vm.State = _wireGuard.GetState(profile.Name);
            Profiles.Add(vm);
        }
        Selected = Profiles.FirstOrDefault();

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _statusTimer.Tick += (_, _) => RefreshUptime();
        _statusTimer.Start();
    }

    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import WireGuard config",
            Filter = "WireGuard config (*.conf)|*.conf|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var profile = _store.Import(dialog.FileName);
            var vm = new ProfileViewModel(profile);
            Profiles.Add(vm);
            Selected = vm;
            SaveProfiles();
            StatusMessage = $"Imported {profile.Name}";
        }
        catch (ConfigParseException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    private async Task ToggleConnectionAsync()
    {
        if (Selected is not { } vm) return;

        try
        {
            if (vm.IsConnected)
            {
                vm.State = TunnelState.Disconnecting;
                await _wireGuard.DisconnectAsync(vm.Profile.Name);
                vm.State = TunnelState.Disconnected;
                _connectStartedAt = null;
                ConnectedSince = "";
                PublicIp = "-";
            }
            else
            {
                vm.State = TunnelState.Connecting;
                await _wireGuard.ConnectAsync(vm.Profile.Name, vm.Profile.ConfigPath);
                vm.State = _wireGuard.GetState(vm.Profile.Name);

                if (vm.IsConnected)
                {
                    _connectStartedAt = DateTime.Now;
                    PublicIp = await _ipInfo.GetPublicIpAsync();
                }
            }

            StatusMessage = null;
        }
        catch (WireGuardNotFoundException ex)
        {
            vm.State = TunnelState.Error;
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            vm.State = TunnelState.Error;
            StatusMessage = $"Connection failed: {ex.Message}";
        }
    }

    private async Task RemoveSelectedAsync()
    {
        if (Selected is not { } vm) return;

        var result = MessageBox.Show(
            $"Remove '{vm.Name}'? This deletes the imported config.",
            "Remove tunnel", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        await _wireGuard.RemoveAsync(vm.Profile.Name);
        _store.DeleteConfig(vm.Profile);
        Profiles.Remove(vm);
        Selected = Profiles.FirstOrDefault();
        SaveProfiles();
    }

    private async Task ToggleKillSwitch(bool enabled)
    {
        try
        {
            if (enabled && Selected != null)
                await _firewall.EnableAsync(Selected.Name);
            else
                await _firewall.DisableAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kill switch failed: {ex.Message}";
        }
    }

    private void RefreshUptime()
    {
        if (_connectStartedAt is not { } start) return;
        var elapsed = DateTime.Now - start;
        ConnectedSince = elapsed.Hours > 0
            ? $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    private void SaveProfiles() => _store.Save(Profiles.Select(p => p.Profile).ToList());
}
