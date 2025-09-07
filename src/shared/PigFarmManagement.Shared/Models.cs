namespace PigFarmManagement.Shared.Models;

/// <summary>
/// Central aggregator for all domain models following Single Responsibility Principle
/// Responsibility: Provide a single entry point for all model imports
/// </summary>

// Import all domain models from their specific responsibility files
// This allows existing code to continue using: using PigFarmManagement.Shared.Models;

// Note: All actual model definitions are now organized by responsibility:
// - Domain/Enums.cs - Business enumerations (CustomerStatus, PigPenType)
// - Domain/Entities.cs - Core domain entities with business logic (Customer, PigPen)
// - Domain/ValueObjects.cs - Immutable value objects (FeedItem, Deposit, HarvestResult)
// - Domain/DTOs.cs - Data transfer objects for internal data exchange (Feed)
// - Domain/Analytics.cs - Summary and statistical models (Dashboard, Import analytics)
// - Domain/External/PosApiModels.cs - External API integration models (POSPOS)
// - Contracts/IFeedImportService.cs - Service contracts and interfaces

// This file serves as a convenience aggregator to maintain backward compatibility
// while organizing code according to Single Responsibility Principle
