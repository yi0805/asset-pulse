using AssetPulse.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetPulse.Api.Data.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets", table =>
        {
            table.HasCheckConstraint(
                "CK_Assets_AssetCode_Uppercase",
                "[AssetCode] COLLATE Latin1_General_100_BIN2 = UPPER([AssetCode]) COLLATE Latin1_General_100_BIN2");
            table.HasCheckConstraint(
                "CK_Assets_RequiredText_Trimmed",
                "LEN([Name]) > 0 AND [Name] = LTRIM(RTRIM([Name])) " +
                "AND LEN([AssetCode]) > 0 AND [AssetCode] = LTRIM(RTRIM([AssetCode])) " +
                "AND LEN([Type]) > 0 AND [Type] = LTRIM(RTRIM([Type])) " +
                "AND LEN([Location]) > 0 AND [Location] = LTRIM(RTRIM([Location]))");
            table.HasCheckConstraint(
                "CK_Assets_Temperature_AbsoluteZero",
                "[Temperature] IS NULL OR [Temperature] >= CONVERT(decimal(10, 2), -273.15)");
            table.HasCheckConstraint(
                "CK_Assets_Pressure_Nonnegative",
                "[Pressure] IS NULL OR [Pressure] >= CONVERT(decimal(10, 2), 0)");
        });

        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.Name).HasMaxLength(100).IsRequired();
        builder.Property(asset => asset.AssetCode).HasMaxLength(32).IsRequired();
        builder.Property(asset => asset.Type).HasMaxLength(50).IsRequired();
        builder.Property(asset => asset.Location).HasMaxLength(100).IsRequired();
        builder.Property(asset => asset.Temperature).HasPrecision(10, 2);
        builder.Property(asset => asset.Pressure).HasPrecision(10, 2);
        builder.Property(asset => asset.LastUpdated).HasColumnType("datetimeoffset").IsRequired();

        builder.HasIndex(asset => asset.AssetCode).IsUnique();

        builder.HasMany(asset => asset.Alarms)
            .WithOne(alarm => alarm.Asset)
            .HasForeignKey(alarm => alarm.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(asset => asset.Events)
            .WithOne(assetEvent => assetEvent.Asset)
            .HasForeignKey(assetEvent => assetEvent.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
