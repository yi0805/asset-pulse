using AssetPulse.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetPulse.Api.Data.Configurations;

public sealed class AlarmConfiguration : IEntityTypeConfiguration<Alarm>
{
    public void Configure(EntityTypeBuilder<Alarm> builder)
    {
        builder.ToTable("Alarms", table =>
        {
            table.HasCheckConstraint("CK_Alarms_Severity", "[Severity] IN ('Warning', 'Critical')");
            table.HasCheckConstraint("CK_Alarms_Status", "[Status] IN ('Active', 'Acknowledged', 'Resolved')");
            table.HasCheckConstraint(
                "CK_Alarms_Message_Trimmed",
                "LEN([Message]) > 0 AND [Message] = LTRIM(RTRIM([Message]))");
            table.HasCheckConstraint(
                "CK_Alarms_Timestamps",
                "([Status] = 'Active' AND [AcknowledgedAt] IS NULL AND [ResolvedAt] IS NULL) " +
                "OR ([Status] = 'Acknowledged' AND [AcknowledgedAt] IS NOT NULL AND [ResolvedAt] IS NULL) " +
                "OR ([Status] = 'Resolved' AND [ResolvedAt] IS NOT NULL)");
        });

        builder.HasKey(alarm => alarm.Id);
        builder.Property(alarm => alarm.Severity).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(alarm => alarm.Message).HasMaxLength(500).IsRequired();
        builder.Property(alarm => alarm.Status).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.Property(alarm => alarm.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(alarm => alarm.AcknowledgedAt).HasColumnType("datetimeoffset");
        builder.Property(alarm => alarm.ResolvedAt).HasColumnType("datetimeoffset");

        builder.HasIndex(alarm => new { alarm.AssetId, alarm.Status });
        builder.HasIndex(alarm => new { alarm.Status, alarm.Severity });
    }
}
