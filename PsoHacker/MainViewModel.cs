using System.Timers;
using MindControl;
using MindControl.Native;
using PropertyChanged.SourceGenerator;
using PsoHacker.Services;
using PsoHacker.ViewModels;

namespace PsoHacker;

public partial class MainViewModel
{
    private readonly ProcessTracker _processTracker = new("Dolphin");
    
    private readonly PsoProcess _psoProcess;
    
    private readonly System.Timers.Timer _attachTimer = new(1000);
    
    [Notify] private bool _isProcessAttached;
    
    [Notify] private string _errorMessage = string.Empty;

    [Notify] private string _successMessage = string.Empty;
    
    public DressingRoomEditorVm DressingRoomEditorVm { get; }
    public MaterialEditorVm MaterialEditorVm { get; }
    
    public MainViewModel()
    {
        _psoProcess = new PsoProcess(_processTracker)
        {
            EmulationMemory = FindMappedMemory(_processTracker.GetProcessMemory())
        };
        _processTracker.Attached += ProcessTrackerOnAttached;
        _processTracker.Detached += ProcessTrackerOnDetached;
        _isProcessAttached = _processTracker.IsAttached;
        _attachTimer.Elapsed += OnAttachTimerTick;
        _attachTimer.Start();
        
        DressingRoomEditorVm = new DressingRoomEditorVm(_psoProcess);
        DressingRoomEditorVm.Search();
        
        MaterialEditorVm = new MaterialEditorVm(_psoProcess);
        MaterialEditorVm.Search();
    }

    private void OnAttachTimerTick(object? sender, ElapsedEventArgs e)
    {
        _processTracker.GetProcessMemory();
    }
    
    private MemoryRange? FindMappedMemory(ProcessMemory? processMemory)
    {
        if (processMemory == null)
            return null;
        
        ClearDisplayMessages();
        var win32 = new Win32Service();
        var handle = processMemory.GetAttachedProcessInstance().Handle;
        UIntPtr address = UIntPtr.Zero;
        try
        {
            MemoryRangeMetadata metadata;
            do
            {
                metadata = win32.GetRegionMetadata(handle, address, true);
                if (metadata.Size == UIntPtr.Zero)
                    break;
                if (metadata is { IsMapped: true, IsCommitted: true } && metadata.Size.ToUInt64() >= 0x2000000)
                    return MemoryRange.FromStartAndSize(metadata.StartAddress, metadata.Size);
                address = metadata.StartAddress + metadata.Size;
            } while (metadata.Size != UIntPtr.Zero);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            UpdateDisplayMessage($"Failed to find the emulation mapped memory: {e.Message}", false);
        }
        
        return null;
    }
    
    private void ProcessTrackerOnAttached(object? sender, EventArgs e)
    {
        IsProcessAttached = true;
        _psoProcess.EmulationMemory = FindMappedMemory(_processTracker.GetProcessMemory());
    }
    
    private void ProcessTrackerOnDetached(object? sender, EventArgs e)
    {
        IsProcessAttached = false;
    }
    
    private void UpdateDisplayMessage(string message, bool isSuccess)
    {
        if (isSuccess)
        {
            ErrorMessage = string.Empty;
            SuccessMessage = message;
        }
        else
        {
            SuccessMessage = string.Empty;
            ErrorMessage = message;
        }
    }

    private void ClearDisplayMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}