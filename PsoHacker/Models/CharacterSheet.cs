namespace PsoHacker.Models;

public class CharacterSheet
{
    public UIntPtr MarkerAddress { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Level { get; init; } = 0;
    public SectionId SectionId { get; init; } = SectionId.Unknown;
}