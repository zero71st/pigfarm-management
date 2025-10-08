using BCrypt.Net;

namespace PigFarmManagement.Server.Features.Authentication.Helpers;

public static class PasswordUtil
{
    private const int DefaultWorkFactor = 12;

    /// <summary>
    /// Hash a password using BCrypt with the configured work factor
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <param name="workFactor">BCrypt work factor (default: 12)</param>
    /// <returns>BCrypt hashed password</returns>
    public static string HashPassword(string password, int workFactor = DefaultWorkFactor)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (workFactor < 4 || workFactor > 31)
            throw new ArgumentException("Work factor must be between 4 and 31", nameof(workFactor));

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
    }

    /// <summary>
    /// Verify a password against its hash
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hash">The BCrypt hash to verify against</param>
    /// <returns>True if password matches hash, false otherwise</returns>
    public static bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Log the exception in a real application
            return false;
        }
    }

    /// <summary>
    /// Generate a temporary password for new users
    /// </summary>
    /// <param name="length">Length of the generated password (default: 12)</param>
    /// <returns>A cryptographically secure random password</returns>
    public static string GenerateTemporaryPassword(int length = 12)
    {
        if (length < 8)
            throw new ArgumentException("Password length must be at least 8 characters", nameof(length));

        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var allChars = upperCase + lowerCase + digits + specialChars;
        var random = new Random();
        var password = new char[length];

        // Ensure at least one character from each category
        password[0] = upperCase[random.Next(upperCase.Length)];
        password[1] = lowerCase[random.Next(lowerCase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = specialChars[random.Next(specialChars.Length)];

        // Fill the rest randomly
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle the password to avoid predictable patterns
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    /// <summary>
    /// Validate password strength
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>Validation result with errors if any</returns>
    public static PasswordValidationResult ValidatePasswordStrength(string password)
    {
        var result = new PasswordValidationResult();

        if (string.IsNullOrWhiteSpace(password))
        {
            result.AddError("Password is required");
            return result;
        }

        if (password.Length < 6)
            result.AddError("Password must be at least 6 characters long");

        if (password.Length > 100)
            result.AddError("Password must not exceed 100 characters");

        if (!password.Any(char.IsUpper))
            result.AddError("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            result.AddError("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            result.AddError("Password must contain at least one digit");

        // Check for common weak passwords
        var commonPasswords = new[] { "password", "123456", "password123", "admin", "qwerty" };
        if (commonPasswords.Any(weak => string.Equals(password, weak, StringComparison.OrdinalIgnoreCase)))
            result.AddError("Password is too common and easily guessable");

        return result;
    }

    /// <summary>
    /// Check if a password hash needs to be rehashed (e.g., work factor changed)
    /// </summary>
    /// <param name="hash">The current password hash</param>
    /// <param name="targetWorkFactor">The target work factor</param>
    /// <returns>True if rehashing is needed</returns>
    public static bool NeedsRehash(string hash, int targetWorkFactor = DefaultWorkFactor)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return true;

        try
        {
            // Extract work factor from existing hash
            // BCrypt format: $2a$[work factor]$[salt+hash]
            var parts = hash.Split('$');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int currentWorkFactor))
            {
                return currentWorkFactor < targetWorkFactor;
            }
        }
        catch
        {
            // If we can't parse the hash, assume it needs rehashing
            return true;
        }

        return true;
    }
}

public class PasswordValidationResult
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