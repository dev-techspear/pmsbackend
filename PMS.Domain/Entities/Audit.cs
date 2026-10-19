namespace PMS.Domain.Entities;

// These four don't extend BaseEntity/AuditableEntity — the source models them with bigint
// PKs and PascalCase table names already shaped for MSSQL (see SOURCE_DATABASE_SCHEMA.md,
// "Audit tables — already MSSQL-shaped in source"), unlike every other table in this
// schema. Ported near-verbatim rather than forced into the common entity shape.

public class AuditFieldsMapping
{
    public long ID { get; set; }
    public required string EntityName { get; set; }
    public required string FieldName { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public bool? IsVisible { get; set; }
    public int? OrderNo { get; set; }
}

public class AuditLogs
{
    public long ID { get; set; }
    public required string TableName { get; set; }
    public int Action { get; set; }
    public Guid PerformedBy { get; set; }
    public DateTime PerformedOn { get; set; }
    public string? RecordName { get; set; }
    public Guid TransactionId { get; set; }
    public string? Comments { get; set; }
    public string? ModuleName { get; set; }

    public ICollection<AuditLogDetails> Details { get; set; } = new List<AuditLogDetails>();
}

public class AuditLogDetails
{
    public long Id { get; set; }
    public long AuditLogId { get; set; }
    public AuditLogs AuditLog { get; set; } = null!;

    public required string FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public int? ValueType { get; set; }
}

public class AuditMetaDataInfo
{
    public int Id { get; set; }
    public string? File_name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ModuleOrEntityName { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
