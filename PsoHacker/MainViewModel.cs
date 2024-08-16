using System.Text;
using System.Timers;
using MindControl;
using MindControl.Native;
using PropertyChanged.SourceGenerator;

namespace PsoHacker;

public partial class MainViewModel
{
    private readonly ProcessTracker _processTracker = new("Dolphin");
    
    private System.Timers.Timer _attachTimer = new(1000);
    
    [Notify] private bool _isProcessAttached;

    [Notify] private bool _isSearching;
    
    [Notify] private string _targetAddress = "<Not found>";
    
    private UIntPtr _targetAddressValue;
    
    [Notify] private string _characterName = "<Unknown>";
    
    [Notify] private string _characterLevel = "<Unknown>";
    
    [Notify] private SectionId _characterSectionId = SectionId.Unknown;
    
    public Dictionary<string, SectionId> SectionIdValues => Enum.GetValues<SectionId>().ToDictionary(x => x.ToString(), x => x);
    
    [Notify] private string _errorMessage = string.Empty;

    [Notify] private string _successMessage = string.Empty;
    
    public bool CanSave => TargetAddress != "<Not found>" && IsProcessAttached && !IsSearching;
    
    public MainViewModel()
    {
        _processTracker.Attached += ProcessTrackerOnAttached;
        _processTracker.Detached += ProcessTrackerOnDetached;
        _isProcessAttached = _processTracker.IsAttached;
        _attachTimer.Elapsed += OnAttachTimerTick;
        _attachTimer.Start();
        Search();
    }

    private void OnAttachTimerTick(object? sender, ElapsedEventArgs e)
    {
        _processTracker.GetProcessMemory();
    }

    public void Search()
    {
        // If we are already searching, return
        if (IsSearching)
            return;
        
        var processMemory = _processTracker.GetProcessMemory(); 
        if (processMemory == null)
            return;

        ClearDisplayMessages();
        IsSearching = true;
        
        TargetAddress = "<Searching>";
        var mappedRange = FindMappedMemory(processMemory);
        _targetAddressValue = mappedRange == null ? 0 : processMemory.FindBytes("00 01 00 1A 00 00 00 00 01 00 00 00 00 00 00 44",
            mappedRange.Value,
            new FindBytesSettings
            {
                SearchExecutable = false,
                SearchMapped = true,
                MaxResultCount = 4
            }).LastOrDefault();

        if (_targetAddressValue == 0)
        {
            TargetAddress = "<Not found>";
            ResetCharacterInfo();
        }
        else
        {
            TargetAddress = _targetAddressValue.ToString("X");
            try
            {
                CharacterName = processMemory.ReadString(_targetAddressValue + 0x80, 16, new StringSettings(Encoding.ASCII, true, null))!;
                
                // Read the level bytes as a big-endian 4-byte integer
                var levelBytes = processMemory.ReadBytes(_targetAddressValue + 0x74, 4)!;
                Array.Reverse(levelBytes);
                CharacterLevel = BitConverter.ToUInt32(levelBytes, 0).ToString();
                
                var characterSectionId = processMemory.ReadByte(_targetAddressValue + 0xB0)!;
                CharacterSectionId = Enum.IsDefined(typeof(SectionId), (int)characterSectionId) ? (SectionId) characterSectionId : SectionId.Unknown;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                UpdateDisplayMessage($"Failed to read data: {e.Message}", false);
                ResetCharacterInfo();
            }
        }
        IsSearching = false;
    }

    private void ResetCharacterInfo()
    {
        CharacterName = "<Unknown>";
        CharacterLevel = "<Unknown>";
        CharacterSectionId = SectionId.Unknown;
    }
    
    private MemoryRange? FindMappedMemory(ProcessMemory processMemory)
    {
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
    
    public void Save()
    {
        if (!CanSave)
            return;
        
        var processMemory = _processTracker.GetProcessMemory();
        if (processMemory == null)
            return;
        
        // Perform some validation
        if (!uint.TryParse(CharacterLevel, out var level) || level <= 0 || level > 200)
        {
            UpdateDisplayMessage("Invalid character level", false);
            return;
        }
        if (CharacterSectionId == SectionId.Unknown)
        {
            UpdateDisplayMessage("Invalid character section ID", false);
            return;
        }

        try
        {
            // Write the character name
            var nameBytes = Encoding.ASCII.GetBytes(CharacterName);
            processMemory.WriteBytes(_targetAddressValue + 0x80, nameBytes, MemoryProtectionStrategy.Ignore);
        
            // Write the character level
            var levelBytes = BitConverter.GetBytes(uint.Parse(_characterLevel));
            Array.Reverse(levelBytes);
            processMemory.WriteBytes(_targetAddressValue + 0x74, levelBytes, MemoryProtectionStrategy.Ignore);
        
            // Write the character section ID
            processMemory.WriteByte(_targetAddressValue + 0xB0, (byte)_characterSectionId, MemoryProtectionStrategy.Ignore);
            
            // If you are reading this wondering how to change the character class, it's a byte at 0xB1:
            // 0 is HUmar, 1 is HUnewearl, and I don't know about the rest but I think 2 does not exist
            
            UpdateDisplayMessage("Character data saved successfully", true);
        }
        catch (Exception e)
        {
            UpdateDisplayMessage($"Failed to save: {e.Message}", false);
            Console.WriteLine(e);
        }
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
    
    private void ProcessTrackerOnAttached(object? sender, EventArgs e)
    {
        IsProcessAttached = true;
    }
    
    private void ProcessTrackerOnDetached(object? sender, EventArgs e)
    {
        IsProcessAttached = false;
    }
}