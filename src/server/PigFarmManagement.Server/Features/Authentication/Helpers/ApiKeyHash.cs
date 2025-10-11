using System.Security.Cryptography;
using System.Text;

namespace PigFarmManagement.Server.Features.Authentication.Helpers;

/// <summary>
/// API Key generation and hashing utilities.
/// SECURITY REVIEW (T010): This implementation uses cryptographically secure methods:
/// - RandomNumberGenerator for key generation (not Random class)
/// - SHA-256 for hashing with optional salting capability
/// - Raw API keys are never stored in database, only hashed values
/// - Proper validation and format checking
/// - Admin password hashing uses ASP.NET Identity PasswordHasher (BCrypt-based)
/// - Generated secrets are only printed once in non-production environments
/// </summary>
public static class ApiKeyHash
{
    private const int KeyLength = 64; // 64 characters for the API key
    private const string KeyPrefix = "pfm_"; // Pig Farm Management prefix

    /// <summary>
    /// Generate a new cryptographically secure API key
    /// </summary>
    /// <returns>A new API key with format: pfm_[64 random characters]</returns>
    public static string GenerateApiKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new char[KeyLength];

        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[KeyLength];
            rng.GetBytes(bytes);

            for (int i = 0; i < KeyLength; i++)
            {
                random[i] = chars[bytes[i] % chars.Length];
            }
        }

        return KeyPrefix + new string(random);
    }

    /// <summary>
    /// Hash an API key using SHA-256 for secure storage
    /// </summary>
    /// <param name="apiKey">The raw API key to hash</param>
    /// <returns>Base64 encoded SHA-256 hash of the API key</returns>
    public static string HashApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

        using (var sha256 = SHA256.Create())
        {
            var keyBytes = Encoding.UTF8.GetBytes(apiKey);
            var hashBytes = sha256.ComputeHash(keyBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Validate an API key against its hash
    /// </summary>
    /// <param name="apiKey">The raw API key to validate</param>
    /// <param name="hash">The stored hash to validate against</param>
    /// <returns>True if the API key matches the hash</returns>
    public static bool ValidateApiKey(string apiKey, string hash)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            var computedHash = HashApiKey(apiKey);
            return string.Equals(computedHash, hash, StringComparison.Ordinal);
        }
        catch
        {
            // Log the exception in a real application
            return false;
        }
    }

    /// <summary>
    /// Validate API key format without checking against a hash
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>Validation result</returns>
    public static ApiKeyValidationResult ValidateApiKeyFormat(string apiKey)
    {
        var result = new ApiKeyValidationResult();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.AddError("API key is required");
            return result;
        }

        if (!apiKey.StartsWith(KeyPrefix))
        {
            result.AddError($"API key must start with '{KeyPrefix}'");
        }

        if (apiKey.Length != KeyLength + KeyPrefix.Length)
        {
            result.AddError($"API key must be exactly {KeyLength + KeyPrefix.Length} characters long");
        }

        // Check if the key part (after prefix) contains only valid characters
        var keyPart = apiKey.Substring(KeyPrefix.Length);
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        
        if (keyPart.Any(c => !validChars.Contains(c)))
        {
            result.AddError("API key contains invalid characters");
        }

        return result;
    }

    /// <summary>
    /// Extract a displayable portion of the API key for logging/UI purposes
    /// </summary>
    /// <param name="apiKey">The full API key</param>
    /// <returns>A safe string for display (first 8 and last 4 characters)</returns>
    public static string GetDisplayableKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 12)
            return "****";

        var prefix = apiKey.Substring(0, Math.Min(8, apiKey.Length));
        var suffix = apiKey.Length > 8 ? apiKey.Substring(apiKey.Length - 4) : "";
        
        return $"{prefix}****{suffix}";
    }

    /// <summary>
    /// Generate multiple API keys for testing or bulk operations
    /// </summary>
    /// <param name="count">Number of keys to generate</param>
    /// <returns>Array of unique API keys</returns>
    public static string[] GenerateMultipleKeys(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        if (count > 1000)
            throw new ArgumentException("Cannot generate more than 1000 keys at once", nameof(count));

        var keys = new string[count];
        var uniqueKeys = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            string key;
            do
            {
                key = GenerateApiKey();
            } while (uniqueKeys.Contains(key));

            uniqueKeys.Add(key);
            keys[i] = key;
        }

        return keys;
    }

    /// <summary>
    /// Create a salted hash for additional security (optional enhancement)
    /// </summary>
    /// <param name="apiKey">The API key to hash</param>
    /// <param name="salt">The salt to use</param>
    /// <returns>Salted hash of the API key</returns>
    public static string HashApiKeyWithSalt(string apiKey, string salt)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

        if (string.IsNullOrWhiteSpace(salt))
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));

        using (var sha256 = SHA256.Create())
        {
            var saltedKey = apiKey + salt;
            var keyBytes = Encoding.UTF8.GetBytes(saltedKey);
            var hashBytes = sha256.ComputeHash(keyBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Generate a cryptographically secure salt
    /// </summary>
    /// <param name="length">Length of the salt in bytes (default: 32)</param>
    /// <returns>Base64 encoded salt</returns>
    public static string GenerateSalt(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentException("Salt length must be positive", nameof(length));

        using (var rng = RandomNumberGenerator.Create())
        {
            var saltBytes = new byte[length];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
    }
}

public class ApiKeyValidationResult
{
    private readonly List<string> _errors = new();

    public bool IsValid => !_errors.Any();
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
            _errors.Add(error);
    }

    public string GetErrorMessage()
    {
        return string.Join("; ", _errors);
    }
}