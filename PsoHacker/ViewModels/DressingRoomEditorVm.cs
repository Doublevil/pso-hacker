using System.Globalization;
using PropertyChanged.SourceGenerator;
using PsoHacker.Models;
using PsoHacker.Services;

namespace PsoHacker.ViewModels;

public partial class DressingRoomEditorVm(PsoProcess psoProcess)
{
    [Notify] private bool _isSearching;
    
    [Notify] private string _targetAddress = "<Not found>";
    
    [Notify] private string _characterName = "<Unknown>";
    
    [Notify] private string _characterLevel = "<Unknown>";
    
    [Notify] private SectionId _characterSectionId = SectionId.Unknown;
    
    public Dictionary<string, SectionId> SectionIdValues => Enum.GetValues<SectionId>().ToDictionary(x => x.ToString(), x => x);
    
    [Notify] private string _errorMessage = string.Empty;

    [Notify] private string _successMessage = string.Empty;

    public bool CanSave => TargetAddress != "<Not found>" && !IsSearching;
    
    public void Search()
    {
        // If we are already searching, return
        if (IsSearching)
            return;
         
        if (!psoProcess.IsProcessAttached)
            return;

        ClearDisplayMessages();
        IsSearching = true;
        
        TargetAddress = "<Searching>";
        try
        {
            var characterSheet = psoProcess.ReadDressingRoomCharacterSheet();
            if (characterSheet == null)
            {
                TargetAddress = "<Not found>";
                ResetCharacterInfo();
            }
            else
            {
                TargetAddress = characterSheet.MarkerAddress.ToString("X");
                CharacterName = characterSheet.Name;
                CharacterLevel = characterSheet.Level.ToString();
                CharacterSectionId = characterSheet.SectionId;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            UpdateDisplayMessage($"Failed to read data: {e.Message}", false);
            ResetCharacterInfo();
        }
        
        IsSearching = false;
    }

    private void ResetCharacterInfo()
    {
        CharacterName = "<Unknown>";
        CharacterLevel = "<Unknown>";
        CharacterSectionId = SectionId.Unknown;
    }
    
    public void Save()
    {
        if (!CanSave)
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
            var characterSheet = new CharacterSheet
            {
                MarkerAddress = UIntPtr.Parse(TargetAddress, NumberStyles.HexNumber),
                Name = CharacterName,
                Level = (int)level,
                SectionId = CharacterSectionId
            };
            psoProcess.WriteDressingRoomCharacterSheet(characterSheet);
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
}