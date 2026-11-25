// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Expressions.Keys;

namespace Cratis.Chronicle.Projections.Engine;

public class when_parsing_flat_composite_keys : Specification
{
    [Theory]
    [InlineData("$flatComposite(a=value)")]
    [InlineData("$flatComposite(a=value,a=other)")]
    [InlineData("$flatComposite(id=value,b=other)")]
    [InlineData("$flatComposite(ID=value,b=other)")]
    [InlineData("$flatComposite(_private=value,b=other)")]
    [InlineData("$flatComposite(a.b=value,c=other)")]
    [InlineData("$flatComposite(a=,b=other)")]
    [InlineData("$flatComposite(a=value,b=other")]
    [InlineData("$flatComposite(a=value,,b=other)")]
    [InlineData("$flatComposite(a=\"unterminated,b=other)")]
    public void should_refuse_a_malformed_or_reserved_shape(string expression) =>
        Catch.Exception(() => FlatCompositeKeyExpression.Parse(expression)).ShouldBeOfExactType<InvalidFlatCompositeKeyExpression>();

    [Fact]
    void should_round_trip_quoted_commas_parentheses_and_generation_qualifiers()
    {
        const string expression = "$flatComposite(z=$literal(\"a,b=(c)\"),a=Changed@2.value)";
        var parsed = FlatCompositeKeyExpression.Parse(expression);
        parsed.Parts[0].Property.ShouldEqual("a");
        FlatCompositeKeyExpression.Parse(FlatCompositeKeyExpression.Format(parsed.Parts)).Parts.ShouldEqual(parsed.Parts);
    }
}
