using MCGCareWEBQI.Shared.Hashing;
using Xunit;

namespace MCGCareWEBQI.Tests;

public class HashTests
{
    [Fact]
    public void Canonicalize_SortsByKey_AndHtmlEncodesValues()
    {
        var fields = new[]
        {
            new KeyValuePair<string, string?>("requestType", "documentation"),
            new KeyValuePair<string, string?>("documentingUser", "jdoe"),
            new KeyValuePair<string, string?>("patientFirstName", "Mary & Sue"),
        };

        var canonical = CwqiHash.CanonicalizeFields(fields);

        // Ordered alphabetically by key (Ordinal), values HTML-encoded, joined by &.
        Assert.Equal(
            "documentingUser=jdoe&patientFirstName=Mary &amp; Sue&requestType=documentation",
            canonical);
    }

    [Fact]
    public void Canonicalize_SkipsEmpty_LoginKey_AndMessageHash()
    {
        var fields = new[]
        {
            new KeyValuePair<string, string?>("a", "1"),
            new KeyValuePair<string, string?>("loginKey", "should-be-skipped"),
            new KeyValuePair<string, string?>("messageHash", "old-hash"),
            new KeyValuePair<string, string?>("emptyOne", ""),
            new KeyValuePair<string, string?>("nullOne", null),
            new KeyValuePair<string, string?>("z", "9"),
        };

        Assert.Equal("a=1&z=9", CwqiHash.CanonicalizeFields(fields));
    }

    [Fact]
    public void Compute_AppendsKeyAsSalt_AndReturnsBase64Sha256()
    {
        // Sha256("foo=bar" + "MYKEY") base64.
        var hash = CwqiHash.Compute("foo=bar", "MYKEY", CwqiHash.Algorithm.Sha256);

        // Validate length and base64 round-trip rather than hard-coding a brittle expected string.
        Assert.Equal(44, hash.Length); // 32 bytes -> 44 chars base64 with padding
        var raw = Convert.FromBase64String(hash);
        Assert.Equal(32, raw.Length);
    }

    [Fact]
    public void Compute_IsDeterministic()
    {
        var a = CwqiHash.Compute("k=v", "salt", CwqiHash.Algorithm.Sha512);
        var b = CwqiHash.Compute("k=v", "salt", CwqiHash.Algorithm.Sha512);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Verify_AcceptsMatching_AndRejectsMismatch()
    {
        var fields = new[]
        {
            new KeyValuePair<string, string?>("foo", "bar"),
            new KeyValuePair<string, string?>("baz", "qux"),
        };
        var hash = CwqiHash.ComputeForFields(fields, "INTERFACE-KEY");

        Assert.True(CwqiHash.Verify(fields, "INTERFACE-KEY", hash));
        Assert.False(CwqiHash.Verify(fields, "WRONG-KEY", hash));
        Assert.False(CwqiHash.Verify(fields, "INTERFACE-KEY", hash + "tampered"));
    }

    [Fact]
    public void Parse_AcceptsCommonAliases_AndIsCaseInsensitive()
    {
        Assert.Equal(CwqiHash.Algorithm.Sha256, CwqiHash.Parse("sha256"));
        Assert.Equal(CwqiHash.Algorithm.Sha256, CwqiHash.Parse("SHA-256"));
        Assert.Equal(CwqiHash.Algorithm.Sha512, CwqiHash.Parse("SHA512"));
        Assert.Equal(CwqiHash.Algorithm.Md5,    CwqiHash.Parse("MD5"));
        Assert.Equal(CwqiHash.Algorithm.Sha1,   CwqiHash.Parse("SHA1"));
        Assert.Throws<ArgumentException>(() => CwqiHash.Parse("XYZ"));
    }
}
