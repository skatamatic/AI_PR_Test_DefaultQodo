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
    // Input: "forbidden_word_here", maxLength: 30, strictMode: false
    // ValidateInputString -> fails
    [TestCase("forbidden_word_here", 30, false, "Error: Initial validation failed.", null)]
    public void PerformComplexFormattingAndValidation_VariousInputs(string input, int maxLength, bool strictMode, string expectedTextContent, string expectedSignaturePart)
    {
        string result = CrossFunctionalHelper.PerformComplexFormattingAndValidation(input, maxLength, strictMode);
        string datePattern = GetExpectedDatePattern();

        if (expectedTextContent == "Error: Initial validation failed.")
        {
            Assert.That(result, Is.EqualTo(expectedTextContent));
        }
        else
        {
            // Check for the presence of a date-like pattern at the beginning
            StringAssert.IsMatch(datePattern, result.Substring(0, Math.Min(result.Length, 18))); // Check beginning for date
            StringAssert.Contains(expectedTextContent, result);

            if (expectedSignaturePart != null)
            {
                StringAssert.Contains(expectedSignaturePart, result);
            }
            else if (input == "short")
            { // Special case for "short" where signature is completely cut
                StringAssert.DoesNotContain("- Signed", result);
            }
        }
    }


    [Test]
    public void GetFormattedTimestamp_ReturnsNonEmptyStringMatchingPattern()
    {
        string timestamp = CrossFunctionalHelper.GetFormattedTimestamp();
        Assert.IsNotEmpty(timestamp);
        // Regex to match "yy-MM-dd HH:mm" or "yyyy-MM-dd HH:mm:ss"
        StringAssert.IsMatch(@"(\d{2}-\d{2}-\d{2} \d{2}:\d{2}|\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", timestamp);
    }

    [Test]
    [TestCase(1000, "VIP", ExpectedResult = 150.00)]
    [TestCase(1000, "PREFERRED", ExpectedResult = 100.00)]
    [TestCase(1000, "STANDARD", ExpectedResult = 50.00)]
    [TestCase(1000, "unknown", ExpectedResult = 50.00)]
    [TestCase(100, "VIP", ExpectedResult = 15.00)]
    [TestCase(2000, "VIP", ExpectedResult = 400.00)]
    public decimal CalculateSpecialDiscount_VariousScenarios(decimal amount, string customerType)
    {
        return CrossFunctionalHelper.CalculateSpecialDiscount(amount, customerType);
    }

    [Test]
    public void TruncateString_TextLongerThanMaxLength_ReturnsTruncatedTextWithEllipsis()
    {
        string text = "This is a long string";
        // "this is a " (10 chars) + "..."
        Assert.That(CrossFunctionalHelper.TruncateString(text, 10), Is.EqualTo("this is a ..."));
    }
}