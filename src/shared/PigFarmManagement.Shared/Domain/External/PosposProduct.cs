using System;

namespace PigFarmManagement.Shared.Domain.External;

/// <summary>
/// DTOs for POSPOS product import and search flows.
/// These are shared contract shapes intended to be used by client and server.
/// </summary>
public class PosposProduct
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Cost { get; set; }
    public PosposCategory? Category { get; set; }
    public PosposUnit? Unit { get; set; }
    public DateTime? LastUpdate { get; set; }
}

public class PosposCategory
{
    public string Name { get; set; } = string.Empty;
}

public class PosposUnit
{
    public string Name { get; set; } = string.Empty;
}
