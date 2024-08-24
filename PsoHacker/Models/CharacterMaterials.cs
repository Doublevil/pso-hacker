namespace PsoHacker.Models;

public class CharacterMaterials
{
    public UIntPtr MarkerAddress { get; init; }
    public byte PowerMaterialCount { get; init; }
    public byte MindMaterialCount { get; init; }
    public byte EvadeMaterialCount { get; init; }
    public byte DefMaterialCount { get; init; }
    public byte LuckMaterialCount { get; init; }
}