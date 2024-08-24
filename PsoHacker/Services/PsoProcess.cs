using System.Text;
using MindControl;
using PsoHacker.Models;

namespace PsoHacker.Services;

public class PsoProcess(ProcessTracker processTracker)
{
    public bool IsProcessAttached => processTracker.IsAttached;
    public MemoryRange? EmulationMemory { get; set; }

    public CharacterSheet? ReadDressingRoomCharacterSheet()
    {
        var processMemory = processTracker.GetProcessMemory();
        if (processMemory == null)
            return null;
        
        var markerAddress = EmulationMemory == null ? 0 : processMemory.FindBytes("00 01 00 1A 00 00 00 00 01 00 00 00 00 00 00 44",
            EmulationMemory,
            new FindBytesSettings
            {
                SearchExecutable = false,
                SearchMapped = true,
                MaxResultCount = 4
            }).LastOrDefault();
        if (markerAddress == UIntPtr.Zero)
            return null;

        return new CharacterSheet
        {
            MarkerAddress = markerAddress,
            Name = processMemory.ReadString(markerAddress + 0x80, 16,
                new StringSettings(Encoding.ASCII, true, null))!,
            Level = ReadBigEndianInt32(processMemory, markerAddress + 0x74),
            SectionId = ReadByteAsEnum(processMemory, markerAddress + 0xB0, SectionId.Unknown),
            
            // If you are reading this wondering how to read the character class, it's a byte at 0xB1:
            // 0 is HUmar, 1 is HUnewearl, and I don't know about the rest but I think 2 does not exist
        };
    }
    
    public void WriteDressingRoomCharacterSheet(CharacterSheet characterSheet)
    {
        var processMemory = processTracker.GetProcessMemory() ?? throw new Exception("Process is detached!");
        
        // Write the character name
        var nameBytes = Encoding.ASCII.GetBytes(characterSheet.Name).Append((byte)0).ToArray();
        processMemory.WriteBytes(characterSheet.MarkerAddress + 0x80, nameBytes, MemoryProtectionStrategy.Ignore);
        
        // Write the character level
        var levelBytes = BitConverter.GetBytes((uint)characterSheet.Level);
        Array.Reverse(levelBytes);
        processMemory.WriteBytes(characterSheet.MarkerAddress + 0x74, levelBytes, MemoryProtectionStrategy.Ignore);
        
        // Write the character section ID
        processMemory.WriteByte(characterSheet.MarkerAddress + 0xB0, (byte)characterSheet.SectionId, MemoryProtectionStrategy.Ignore);
    }

    public CharacterMaterials? ReadCharacterMaterials()
    {
        var processMemory = processTracker.GetProcessMemory();
        if (processMemory == null)
            return null;
        
        var markerAddress = EmulationMemory == null ? 0 : processMemory.FindBytes("8B BC 80 DD DE 18 00 00 01 F0",
            EmulationMemory,
            new FindBytesSettings
            {
                SearchExecutable = false,
                SearchMapped = true,
                MaxResultCount = 1
            }).FirstOrDefault();
        if (markerAddress == UIntPtr.Zero)
            return null;

        return new CharacterMaterials
        {
            MarkerAddress = markerAddress,
            PowerMaterialCount = processMemory.ReadByte(markerAddress + 0x1E) ?? 0,
            MindMaterialCount = processMemory.ReadByte(markerAddress + 0x1F) ?? 0,
            EvadeMaterialCount = processMemory.ReadByte(markerAddress + 0x20) ?? 0,
            DefMaterialCount = processMemory.ReadByte(markerAddress + 0x21) ?? 0,
            LuckMaterialCount = processMemory.ReadByte(markerAddress + 0x22) ?? 0
        };
    }
    
    public void WriteCharacterMaterials(CharacterMaterials characterMaterials)
    {
        var processMemory = processTracker.GetProcessMemory() ?? throw new Exception("Process is detached!");
        
        processMemory.WriteByte(characterMaterials.MarkerAddress + 0x1E, characterMaterials.PowerMaterialCount, MemoryProtectionStrategy.Ignore);
        processMemory.WriteByte(characterMaterials.MarkerAddress + 0x1F, characterMaterials.MindMaterialCount, MemoryProtectionStrategy.Ignore);
        processMemory.WriteByte(characterMaterials.MarkerAddress + 0x20, characterMaterials.EvadeMaterialCount, MemoryProtectionStrategy.Ignore);
        processMemory.WriteByte(characterMaterials.MarkerAddress + 0x21, characterMaterials.DefMaterialCount, MemoryProtectionStrategy.Ignore);
        processMemory.WriteByte(characterMaterials.MarkerAddress + 0x22, characterMaterials.LuckMaterialCount, MemoryProtectionStrategy.Ignore);
    }
    
    private TEnum ReadByteAsEnum<TEnum>(ProcessMemory processMemory, UIntPtr address, TEnum defaultValue) where TEnum : Enum
    {
        var value = processMemory.ReadByte(address);
        if (value == null)
            return defaultValue;
        
        return Enum.IsDefined(typeof(TEnum), (int)value) ? (TEnum) Enum.ToObject(typeof(TEnum), value) : defaultValue;
    }
    
    private int ReadBigEndianInt32(ProcessMemory processMemory, UIntPtr address)
    {
        var bytes = processMemory.ReadBytes(address, 4)!;
        Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }
}