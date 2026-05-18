using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MCGCareWEBQI.Shared.Hashing;

/// Per CareWebQI Developer's Guide §2.2:
/// 1. Order key/value pairs alphabetically by key name.
/// 2. HTML-encode (UTF-8) the values, format as key=value joined with &.
/// 3. Append the shared interface key (NOT a field, used as raw salt).
/// 4. Hash with SHA-256 or SHA-512.
/// 5. Base64-encode the hash. That value is sent as messageHash=...
public static class CwqiHash
{
    public enum Algorithm { Sha1, Sha256, Sha512, Md5 }

    public static Algorithm Parse(string? name) => (name ?? "SHA256").Trim().ToUpperInvariant() switch
    {
        "SHA1" => Algorithm.Sha1,
        "SHA256" or "SHA-256" => Algorithm.Sha256,
        "SHA512" or "SHA-512" => Algorithm.Sha512,
        "MD5" => Algorithm.Md5,
        _ => throw new ArgumentException($"Unsupported hash algorithm '{name}'.")
    };

    /// Build the canonical plaintext from fields (skip empties, sort by key, HTML-encode values, &-join).
    public static string CanonicalizeFields(IEnumerable<KeyValuePair<string, string?>> fields)
    {
        var ordered = fields
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Where(kv => !string.Equals(kv.Key, "loginKey", StringComparison.OrdinalIgnoreCase))
            .Where(kv => !string.Equals(kv.Key, "messageHash", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={WebUtility.HtmlEncode(kv.Value)}");
        return string.Join("&", ordered);
    }

    /// Compute the messageHash value (base64-encoded digest of canonical plaintext + interface key).
    public static string Compute(string canonicalPlainText, string interfaceKey, Algorithm algorithm = Algorithm.Sha256)
    {
        ArgumentNullException.ThrowIfNull(canonicalPlainText);
        ArgumentNullException.ThrowIfNull(interfaceKey);

        var bytes = Encoding.ASCII.GetBytes(canonicalPlainText + interfaceKey);
        var digest = algorithm switch
        {
            Algorithm.Sha1   => SHA1.HashData(bytes),
            Algorithm.Sha256 => SHA256.HashData(bytes),
            Algorithm.Sha512 => SHA512.HashData(bytes),
            Algorithm.Md5    => MD5.HashData(bytes),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
        return Convert.ToBase64String(digest);
    }

    /// One-shot helper for the common path: build canonical plaintext from fields and hash with key.
    public static string ComputeForFields(
        IEnumerable<KeyValuePair<string, string?>> fields,
        string interfaceKey,
        Algorithm algorithm = Algorithm.Sha256)
        => Compute(CanonicalizeFields(fields), interfaceKey, algorithm);

    /// Validate a hash supplied by a counterparty. Constant-time comparison.
    public static bool Verify(
        IEnumerable<KeyValuePair<string, string?>> fields,
        string interfaceKey,
        string suppliedHash,
        Algorithm algorithm = Algorithm.Sha256)
    {
        var expected = ComputeForFields(fields, interfaceKey, algorithm);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(suppliedHash ?? string.Empty));
    }
}
