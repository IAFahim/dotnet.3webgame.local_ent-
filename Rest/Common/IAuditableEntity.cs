namespace Rest.Common;

/// <summary>
/// Represents an entity that can be audited for creation and modification.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the entity was last modified.
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}
