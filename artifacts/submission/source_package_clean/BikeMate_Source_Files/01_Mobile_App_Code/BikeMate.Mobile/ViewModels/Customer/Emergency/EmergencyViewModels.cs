using BikeMate.Core.DTOs;
using BikeMate.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BikeMate.ViewModels.Customer.Emergency;

internal sealed partial class EmergencySosViewModel : ObservableObject
{
    [ObservableProperty]
    private EmergencyLocationSnapshot? location;

    [ObservableProperty]
    private string notes = "";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "";
}

internal sealed partial class CallingEmergencyViewModel : ObservableObject
{
    [ObservableProperty]
    private EmergencyRequestStatusDto? status;

    [ObservableProperty]
    private int elapsedSeconds;

    [ObservableProperty]
    private bool isBusy;
}

internal sealed partial class EmergencyLiveCallViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isMuted;

    [ObservableProperty]
    private bool isCameraEnabled = true;

    [ObservableProperty]
    private bool isSpeakerEnabled = true;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Connecting with BikeMate support. Stay on the line.";
}

internal sealed partial class EmergencyLocationPickerViewModel : ObservableObject
{
    [ObservableProperty]
    private string address = "";

    [ObservableProperty]
    private decimal latitude;

    [ObservableProperty]
    private decimal longitude;

    [ObservableProperty]
    private string notes = "";
}

internal sealed partial class ActiveEmergencyTrackingViewModel : ObservableObject
{
    [ObservableProperty]
    private EmergencyRequestStatusDto? status;

    [ObservableProperty]
    private string banner = "";
}
