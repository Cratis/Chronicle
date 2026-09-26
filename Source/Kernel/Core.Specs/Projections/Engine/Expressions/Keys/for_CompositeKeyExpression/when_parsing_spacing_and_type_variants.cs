// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpression;

public class when_parsing_spacing_and_type_variants : Specification
{
    [Theory]
    [InlineData("$composite(Key, first=one,second=two)", "Key")]
    [InlineData("$composite( Key , first = one , second = two )", "Key")]
    [InlineData("$composite(first=one, second=two)", null)]
    [InlineData("$composite(first=one,second=two)", null)]
    void should_parse_both_mappings(string expression, string? typeName)
    {
        var parsed = CompositeKeyExpression.Parse(expression);
        parsed.TypeName.ShouldEqual(typeName);
        parsed.Mappings.Count.ShouldEqual(2);
        parsed.Mappings[0].ShouldEqual(("first", "one"));
        parsed.Mappings[1].ShouldEqual(("second", "two"));
    }

    [Theory]
    [InlineData("$composite()")]
    [InlineData("$composite(Key)")]
    [InlineData("$composite(Key, first=one,)")]
    [InlineData("$composite(first=)")]
    [InlineData("$composite(first=one, Wrong, second=two)")]
    [InlineData("$composite(first=one=two)")]
    void should_reject_malformed_input(string expression) =>
        Catch.Exception(() => CompositeKeyExpression.Parse(expression)).ShouldBeOfExactType<InvalidCompositeKeyExpression>();
}
