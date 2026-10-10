// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance;

/// <summary>
/// Compiles every corpus case through Chronicle's <see cref="LanguageService"/> and through Screenplay's executable semantic
/// model, and requires both to lower it the same way unless <see cref="ConformanceExceptions"/> lists the difference.
/// </summary>
public class when_lowering_a_declaration_both_ways
{
    public static TheoryData<string> Cases => new(ConformanceCase.Names);

    [Theory]
    [MemberData(nameof(Cases))]
    public void should_lower_equivalently_or_differ_as_listed(string @case)
    {
        var result = ConformanceCase.Load(@case).Compare();

        if (ConformanceExceptions.All.TryGetValue(@case, out var exception))
        {
            Assert.True(
                exception.Matches(result.Outcome),
                $"'{@case}' is listed as {exception.Direction} ({exception.Reason}) but no longer differs that way. Update or remove the entry.\n{result.Describe()}");
            return;
        }

        Assert.True(result.Outcome == ConformanceOutcome.Equivalent, $"'{@case}' lowers differently and is not listed.\n{result.Describe()}");
        Assert.False(string.IsNullOrWhiteSpace(result.ChronicleTree), $"'{@case}' lowers to nothing, so its equivalence proves nothing.");
    }

    [Fact]
    public void should_list_only_cases_in_the_corpus() =>
        Assert.Empty(ConformanceExceptions.All.Keys.Except(ConformanceCase.Names));

    [Fact]
    public void should_give_a_reason_for_every_listed_difference() =>
        Assert.Empty(ConformanceExceptions.All.Where(_ => string.IsNullOrWhiteSpace(_.Value.Reason)).Select(_ => _.Key));
}
