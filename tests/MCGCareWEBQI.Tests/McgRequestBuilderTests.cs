using MCGCareWEBQI.Shared.Configuration;
using MCGCareWEBQI.Shared.Hashing;
using MCGCareWEBQI.Shared.Models.Launch;
using MCGCareWEBQI.Shared.Models.Mcg;
using Xunit;

namespace MCGCareWEBQI.Tests;

public class McgRequestBuilderTests
{
    private static McgOptions Opts() => new()
    {
        InterfaceLoginUrl = "http://mock/interface/interfacelogin.aspx",
        WebServicesUrl    = "http://mock/WebServices/Reconcile.asmx",
        LoginKey          = "TEST-KEY-12345",
        HashAlgorithm     = "SHA256",
        RequestVersion    = "12.0",
        IsInteractive     = true,
        DefaultRequestType = "documentation",
    };

    [Fact]
    public void Build_Includes_MessageHash_That_Verifies()
    {
        var launch = new LaunchRequest
        {
            CallerId         = "test",
            CallerTxnId      = "T-1",
            PatientId        = "P-1",
            PatientFirstName = "Jane",
            PatientLastName  = "Doe",
            PatientGender    = "Female",
            EpisodeType      = "Inpatient",
        };

        var mcg    = Opts();
        var fields = McgRequestBuilder.Build(launch, mcg, "http://bridge/receive", cwqiTransactionId: "REQ-1");

        var dict = fields.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Contains("messageHash", dict.Keys);
        Assert.Equal("documentation", dict["requestType"]);
        Assert.Equal("12.0",          dict["requestVersion"]);
        Assert.Equal("REQ-1",         dict["requestID"]);
        Assert.Equal("http://bridge/receive", dict["returnUrl"]);

        // The hash MCG sees should verify with the same fields and key.
        var nonHashFields = fields
            .Where(kv => kv.Key != "messageHash")
            .Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value));
        Assert.True(CwqiHash.Verify(nonHashFields, mcg.LoginKey, dict["messageHash"], CwqiHash.Algorithm.Sha256));
    }

    [Fact]
    public void Build_Skips_Null_Fields()
    {
        var launch = new LaunchRequest { CallerId = "x", CallerTxnId = "y" };
        var fields = McgRequestBuilder.Build(launch, Opts(), "http://bridge/receive");
        var dict   = fields.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.DoesNotContain("patientID", dict.Keys);
        Assert.DoesNotContain("facilityName", dict.Keys);
        Assert.DoesNotContain("pcpID", dict.Keys);
    }

    [Fact]
    public void Build_Routes_RequestType_From_Launch_Override()
    {
        var launch = new LaunchRequest { CallerId = "x", CallerTxnId = "y", RequestType = "guideline" };
        var fields = McgRequestBuilder.Build(launch, Opts(), "http://bridge/receive");
        var dict   = fields.ToDictionary(kv => kv.Key, kv => kv.Value);
        Assert.Equal("guideline", dict["requestType"]);
    }
}
