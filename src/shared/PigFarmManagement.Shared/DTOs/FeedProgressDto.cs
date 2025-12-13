namespace PigFarmManagement.Shared.DTOs;

/// <summary>
/// DTO for feed progress tracking per pig pen.
/// Shows accumulated vs expected feed quantities and latest import date.
/// </summary>
public record FeedProgressDto(
    Guid PigPenId,
    int AccumulatedBags,
    int ExpectedBags,
    double ProgressPercent,
    DateTime? LastImportDate
);
