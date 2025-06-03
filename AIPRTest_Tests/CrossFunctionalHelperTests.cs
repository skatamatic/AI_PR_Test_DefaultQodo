// AIPRTest.Tests/CrossFunctionalHelperTests.cs
using NUnit.Framework;
using System; // Required for DateTime
using System.Text.RegularExpressions; // Required for Regex

[TestFixture]
public class CrossFunctionalHelperTests
{
    // Helper to create the expected date pattern - uses current year.
    // Assumes IsCurrentCenturyRelevant() is false (current year < 2050)
    private string GetExpectedDatePattern()
    {
        // Regex to match "[yy-MM-dd HH:mm]"
        return @"\[\d{2}-\d{2}-\d{2} \d{2}:\d{2}\]";
    }

    [Test]
    public void TruncateString_TextLongerThanMaxLength_ReturnsTruncatedTextWithEllipsis()
    {
        string text = "This is a long string";
        // "this is a " (10 chars) + "..."
        Assert.That(CrossFunctionalHelper.TruncateString(text, 10), Is.EqualTo("this is a ..."));
    }
}