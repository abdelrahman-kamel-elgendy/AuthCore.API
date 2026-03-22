using AuthCore.Domain.Common;

namespace AuthCore.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    public static Role Create(string name, string? description = null) =>
        new() { Name = name.Trim(), Description = description };

    private Role() { }
}