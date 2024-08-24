using System.Globalization;
using PropertyChanged.SourceGenerator;
using PsoHacker.Models;
using PsoHacker.Services;

namespace PsoHacker.ViewModels;

public partial class MaterialEditorVm(PsoProcess psoProcess)
{
    [Notify] private bool _isSearching;
    
    [Notify] private string _errorMessage = string.Empty;

    [Notify] private string _successMessage = string.Empty;
    
    [Notify] private string _targetAddress = "<Not found>";
    
    [Notify] private string _powerMaterialCount = "0";
    
    [Notify] private string _mindMaterialCount = "0";
    
    [Notify] private string _evadeMaterialCount = "0";
    
    [Notify] private string _defMaterialCount = "0";
    
    [Notify] private string _luckMaterialCount = "0";
    
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
            var materials = psoProcess.ReadCharacterMaterials();
            if (materials == null)
            {
                TargetAddress = "<Not found>";
                ResetData();
            }
            else
            {
                TargetAddress = materials.MarkerAddress.ToString("X");
                PowerMaterialCount = materials.PowerMaterialCount.ToString();
                MindMaterialCount = materials.MindMaterialCount.ToString();
                EvadeMaterialCount = materials.EvadeMaterialCount.ToString();
                DefMaterialCount = materials.DefMaterialCount.ToString();
                LuckMaterialCount = materials.LuckMaterialCount.ToString();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            UpdateDisplayMessage($"Failed to read data: {e.Message}", false);
            ResetData();
        }
        
        IsSearching = false;
    }

    private void ResetData()
    {
        TargetAddress = "<Unknown>";
        PowerMaterialCount = "0";
        MindMaterialCount = "0";
        EvadeMaterialCount = "0";
        DefMaterialCount = "0";
        LuckMaterialCount = "0";
    }
    
    public void Save()
    {
        if (!CanSave)
            return;
        
        // Perform some validation
        if (!byte.TryParse(PowerMaterialCount, out var powerMaterialCount))
        {
            UpdateDisplayMessage("Invalid power material count", false);
            return;
        }
        if (!byte.TryParse(MindMaterialCount, out var mindMaterialCount))
        {
            UpdateDisplayMessage("Invalid mind material count", false);
            return;
        }
        if (!byte.TryParse(EvadeMaterialCount, out var evadeMaterialCount))
        {
            UpdateDisplayMessage("Invalid evasion material count", false);
            return;
        }
        if (!byte.TryParse(DefMaterialCount, out var defMaterialCount))
        {
            UpdateDisplayMessage("Invalid defense material count", false);
            return;
        }
        if (!byte.TryParse(LuckMaterialCount, out var luckMaterialCount))
        {
            UpdateDisplayMessage("Invalid luck material count", false);
            return;
        }

        try
        {
            var characterSheet = new CharacterMaterials
            {
                MarkerAddress = UIntPtr.Parse(TargetAddress, NumberStyles.HexNumber),
                PowerMaterialCount = powerMaterialCount,
                MindMaterialCount = mindMaterialCount,
                EvadeMaterialCount = evadeMaterialCount,
                DefMaterialCount = defMaterialCount,
                LuckMaterialCount = luckMaterialCount
            };
            psoProcess.WriteCharacterMaterials(characterSheet);
            UpdateDisplayMessage("Materials saved successfully", true);
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