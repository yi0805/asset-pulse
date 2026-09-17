using AssetPulse.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetPulse.Api.Data.Configurations;

public sealed class AssetEventConfiguration : IEntityTypeConfiguration<AssetEvent>
{
    public void Configure(EntityTypeBuilder<AssetEvent> builder)
    {
        builder.ToTable("AssetEvents", table =>
        {
            table.HasCheckConstraint(
                "CK_AssetEvents_PreviousStatus",
                "[PreviousStatus] IS NULL OR [PreviousStatus] IN ('Healthy', 'Warning', 'Critical')");
            table.HasCheckConstraint(
                "CK_AssetEvents_NewStatus",
                "[NewStatus] IN ('Healthy', 'Warning', 'Critical')");
            table.HasCheckConstraint(
                "CK_AssetEvents_Description_Trimmed",
                "LEN([Description]) > 0 AND [Description] = LTRIM(RTRIM([Description]))");
        });

        builder.HasKey(assetEvent => assetEvent.Id);
        builder.Property(assetEvent => assetEvent.PreviousStatus).HasConversion<string>().HasMaxLength(8);
        builder.Property(assetEvent => assetEvent.NewStatus).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(assetEvent => assetEvent.Timestamp).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(assetEvent => assetEvent.Description).HasMaxLength(500).IsRequired();

        builder.HasIndex(assetEvent => new { assetEvent.AssetId, assetEvent.Timestamp });
    }
}
