namespace GS.Core.Abstractions;

public interface ICategoryEntity
{
    string Name { get; set; }
    string Code { get; set; }
    string? Description { get; set; }
    bool IsActive { get; set; }
}