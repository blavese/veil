using Veil.Models;
using Veil.Services;

namespace Veil.ViewModels;

public class ProfileViewModel : ObservableObject
{
    public TunnelProfile Profile { get; }

    private TunnelState _state = TunnelState.Disconnected;
    public TunnelState State
    {
        get => _state;
        set
        {
            if (Set(ref _state, value))
            {
                Raise(nameof(IsConnected));
                Raise(nameof(StatusText));
            }
        }
    }

    public bool IsConnected => State == TunnelState.Connected;

    public string StatusText => State switch
    {
        TunnelState.Connected => "Connected",
        TunnelState.Connecting => "Connecting…",
        TunnelState.Disconnecting => "Disconnecting…",
        TunnelState.Error => "Error",
        _ => "Disconnected",
    };

    public string Name => Profile.Name;
    public string Endpoint => Profile.Endpoint;

    public ProfileViewModel(TunnelProfile profile)
    {
        Profile = profile;
    }
}
