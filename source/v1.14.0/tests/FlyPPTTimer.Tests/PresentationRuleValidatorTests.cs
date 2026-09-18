using FlyPPTTimer.Services;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Tests;

public sealed class PresentationRuleValidatorTests
{
    [Theory]
    [InlineData("00:00:01", "00:00:01")]
    [InlineData(" 01:02:03 ", "01:02:03")]
    [InlineData("23:59:59", "23:59:59")]
    public void StrictDuration_OnlyAcceptsPositiveHourMinuteSecondValues(string input, string expected)
    {
        Assert.True(PresentationRuleValidator.TryNormalizeDuration(input, out var normalized, out var error));
        Assert.Equal(expected, normalized);
        Assert.Equal("", error);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("00:01")]
    [InlineData("1:02:03")]
    [InlineData("24:00:00")]
    [InlineData("abc")]
    public void StrictDuration_RejectsIncompleteOrNonPositiveValues(string input)
    {
        Assert.False(PresentationRuleValidator.TryNormalizeDuration(input, out _, out var error));
        Assert.NotEmpty(error);
    }

    [Fact]
    public void NormalizedPathAndId_AreStableAcrossEquivalentPathCasing()
    {
        var left = @"C:\Slides\Demo.pptx";
        var right = @"c:\slides\demo.pptx";
        Assert.Equal(PresentationRuleValidator.IdForPath(left), PresentationRuleValidator.IdForPath(right));
    }

    [Fact]
    public void SelectedRuleId_ResolvesOnlyTheSelectedEnabledPresentation()
    {
        var first = @"C:\Slides\A.pptx";
        var second = @"C:\Slides\B.pptx";
        var rules = new[]
        {
            new FileRule { FileName = "A", FilePath = first, Enabled = true },
            new FileRule { FileName = "B", FilePath = second, Enabled = true }
        };

        Assert.True(PresentationRuleValidator.TryResolveEnabledRule(rules, PresentationRuleValidator.IdForPath(second), out var resolved, out _));
        Assert.Equal(PresentationRuleValidator.NormalizePath(second).ToUpperInvariant(), resolved.ToUpperInvariant());
        Assert.False(string.Equals(PresentationRuleValidator.NormalizePath(first), resolved, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DisabledRule_CannotBeResolvedForOpeningOrSlideshow()
    {
        var rule = new FileRule { FileName = "A", FilePath = @"C:\Slides\A.pptx", Enabled = false };
        Assert.False(PresentationRuleValidator.TryResolveEnabledRule([rule], PresentationRuleValidator.IdForPath(rule.FilePath), out _, out var error));
        Assert.Contains("已启用", error);
    }

    [Fact]
    public void MergeList_AddsOpenNonRulePresentationWithoutDuplicatingRulePath()
    {
        var rule = new FileRule { FileName = "Rule", FilePath = @"C:\Slides\Rule.pptx", Enabled = true };
        var openRule = new PresentationOption { Name = "Rule.pptx", Directory = @"C:\Slides", IsOpen = true };
        var openOther = new PresentationOption { Name = "Other.pptx", Directory = @"C:\Slides", IsOpen = true, IsActive = true };

        var entries = PresentationRuleValidator.MergeRulesAndOpenPresentations([rule], [openRule, openOther]);

        Assert.Equal(2, entries.Count);
        Assert.Single(entries, entry => entry.Rule is not null);
        Assert.Single(entries, entry => entry.Rule is null && entry.Presentation?.Name == "Other.pptx");
    }
}
