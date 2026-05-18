using MCGCareWEBQI.Shared.Models.Mcg;
using Xunit;

namespace MCGCareWEBQI.Tests;

public class CwqiMessageTests
{
    private const string SampleXml = """
        <CwqiMessage requestID="REQ-42">
          <Episode EpisodeId="EPS-1" RequestType="documentation" DocumentingUser="jdoe">
            <Patient PatientId="P-1" FirstName="Jane" LastName="Doe" DateOfBirth="1980-01-15" Gender="Female" />
            <EpisodeNotes>
              <Note Author="jdoe" Created="2026-05-18T10:00:00Z">Notes go here</Note>
            </EpisodeNotes>
            <Guidelines>
              <Guideline GuidelineId="G-1" Title="Inpatient Care">
                <Outlines>
                  <Outline Name="Severity" Status="Met" />
                  <Outline Name="Intensity" Status="NotMet">
                    <Notes><Note Author="jdoe" Created="2026-05-18T10:01:00Z">Insufficient.</Note></Notes>
                  </Outline>
                </Outlines>
              </Guideline>
            </Guidelines>
          </Episode>
        </CwqiMessage>
        """;

    [Fact]
    public void Parses_All_Layers_Of_The_Document()
    {
        var msg = CwqiMessage.Parse(SampleXml);
        Assert.Equal("REQ-42", msg.RequestId);
        Assert.Equal("EPS-1",  msg.EpisodeId);
        Assert.NotNull(msg.Patient);
        Assert.Equal("P-1",    msg.Patient!.PatientId);
        Assert.Equal("Doe",    msg.Patient.LastName);
        Assert.Single(msg.EpisodeNotes);
        Assert.Equal("Notes go here", msg.EpisodeNotes[0].Text);

        var g = Assert.Single(msg.Guidelines);
        Assert.Equal("G-1", g.GuidelineId);
        Assert.Equal(2, g.Outlines.Count);
        Assert.Equal("Met",    g.Outlines[0].Status);
        Assert.Equal("NotMet", g.Outlines[1].Status);
        Assert.Equal("Insufficient.", g.Outlines[1].Notes[0].Text);
    }

    [Fact]
    public void Error_TryParse_Recognizes_Cwqierror_Envelope()
    {
        const string errXml = """
            <cwqierror type="CWQI.DataException" sourceip="10.37.3.156" requestid="REQ-99">
              <messages>
                <message code="12012">Patient First Name may not be empty.</message>
                <message code="12015">Patient Last Name may not be empty.</message>
              </messages>
            </cwqierror>
            """;
        Assert.True(CwqiError.TryParse(errXml, out var err));
        Assert.NotNull(err);
        Assert.Equal("REQ-99", err!.RequestId);
        Assert.Equal(2, err.Messages.Count);
        Assert.Equal("12012", err.Messages[0].Code);
    }

    [Fact]
    public void Error_TryParse_Rejects_Non_Error_Xml()
    {
        Assert.False(CwqiError.TryParse(SampleXml, out var err));
        Assert.Null(err);
    }
}
