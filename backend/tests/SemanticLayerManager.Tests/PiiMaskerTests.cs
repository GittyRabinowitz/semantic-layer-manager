using SemanticLayerManager.Api.Application.DataAccess;

namespace SemanticLayerManager.Tests;

public class PiiMaskerTests
{
    [Fact]
    public void Null_StaysNull()
    {
        Assert.Null(PiiMasker.Mask(null));
    }

    [Fact]
    public void Empty_StaysEmpty()
    {
        Assert.Equal("", PiiMasker.Mask(""));
    }

    [Fact]
    public void Email_KeepsPrefixAndDomain()
    {
        Assert.Equal("no******@example.com", PiiMasker.Mask("noa.levi@example.com"));
    }

    [Fact]
    public void Generic_KeepsTwoLeadingChars()
    {
        Assert.Equal("Is****", PiiMasker.Mask("Israel"));
    }

    [Fact]
    public void ShortValue_IsStillMasked()
    {
        Assert.Equal("A***", PiiMasker.Mask("A"));
    }

    [Fact]
    public void NonStringValue_IsMaskedByItsText()
    {
        // A numeric PII value (e.g. an id) is masked by its string form.
        Assert.Equal("12***", PiiMasker.Mask(12345));
    }
}
