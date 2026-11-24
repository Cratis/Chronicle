// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_arithmetic_factors : given.a_runtime_language_service
{
    IDictionary<string, object?> _state;

    async Task Because()
    {
        await Compile("""
            from Changed
              add age by left * right
              subtract total by -0.1 * 20
            """);
        _state = Project(new { left = 1.5m, right = 2m }, new ExpandoObject()).CurrentState;
    }

    [Fact] void should_multiply_before_coercing_to_an_integer() => _state["age"].ShouldEqual(3);
    [Fact] void should_subtract_a_decimal_product() => _state["total"].ShouldEqual(2m);

    [Theory]
    [InlineData("0.1 * 0.2", "0.02")]
    [InlineData("left * 0", "0")]
    [InlineData("$eventContext.sequenceNumber * 2", "10")]
    public async Task should_preserve_decimal_precision_and_context_expressions(string expression, string expected)
    {
        await Compile($"from Changed\n  add total by {expression}");
        var result = Project(new { left = 1.5m }, new { total = 0m }.AsExpandoObject()).CurrentState;
        ((IDictionary<string, object?>)result)["total"].ShouldEqual(decimal.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task should_use_invariant_numeric_literals_under_a_decimal_comma_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            CultureInfo.CurrentCulture = culture;
            await Compile("from Changed\n  add total by -0.1 * 0.2");
            var result = Project(new { left = 0m }, new ExpandoObject()).CurrentState;
            ((IDictionary<string, object?>)result)["total"].ShouldEqual(-0.02m);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("text")]
    [InlineData("overflow")]
    public async Task should_fail_instead_of_silently_using_zero(string kind)
    {
        await Compile("from Changed\n  add total by left * right");
        object content = kind switch
        {
            "null" => new { left = (decimal?)null, right = 1m },
            "text" => new { left = "not numeric", right = 1m },
            "overflow" => new { left = decimal.MaxValue, right = 2m },
            _ => new { right = 1m }
        };
        Catch.Exception(() => Project(content, new ExpandoObject())).ShouldNotBeNull();
    }
}
