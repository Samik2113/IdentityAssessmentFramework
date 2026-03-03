using FluentAssertions;

namespace IamMaturityStudio.Tests;

public class SmokeTests
{
    [Fact]
    public void True_ShouldBeTrue()
    {
        true.Should().BeTrue();
    }
}