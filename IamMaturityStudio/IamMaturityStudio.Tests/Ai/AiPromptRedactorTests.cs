using IamMaturityStudio.Infrastructure.Services;
using FluentAssertions;

namespace IamMaturityStudio.Tests.Ai;

public class AiPromptRedactorTests
{
    private readonly AiPromptRedactor _redactor = new();

    [Fact]
    public void Redacts_Emails_Urls_Phones_Guids_Ips_And_OrgMarkers()
    {
        var input = "Contact john.doe@contoso.com at https://contoso.com or +1 (425) 555-1212. Ref 3f2504e0-4f89-11d3-9a0c-0305e82c3301 from 10.12.0.8. Organization: Contoso Bank";

        var result = _redactor.Redact(input);

        result.Should().Contain("[REDACTED_EMAIL]");
        result.Should().Contain("[REDACTED_URL]");
        result.Should().Contain("[REDACTED_PHONE]");
        result.Should().Contain("[REDACTED_GUID]");
        result.Should().Contain("[REDACTED_IP]");
        result.Should().Contain("Organization:[REDACTED_ORG]");
    }
}
