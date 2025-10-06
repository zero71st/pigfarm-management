namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Business enumerations for the PigFarm domain
/// Responsibility: Define business enumeration values and their meanings
/// </summary>

public enum CustomerStatus
{
    Active,
    Inactive
}

public enum PigPenType
{
    Project,
    Cash
}

public enum DepositCompletionStatus
{
    None,      // No deposits made
    Started,   // Some deposits but less than 50%
    Partial,   // 50% or more but not complete
    Complete   // 100% or more of expected deposit
}

public enum HarvestCompletionStatus
{
    None,      // No harvests made
    Started,   // Some pigs harvested but less than 50%
    Partial,   // 50% or more but not complete
    Complete   // All pigs harvested
}
