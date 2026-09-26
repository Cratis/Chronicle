// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpression;

public class when_parsing_a_legacy_expression : Specification
{
    CompositeKeyExpression _result;

    void Because() => _result = CompositeKeyExpression.Parse("$composite(first=one,second = two)", (ProjectionId)"projection", PropertyPath.NotSet);

    [Fact] void should_have_no_type() => _result.TypeName.ShouldBeNull();
    [Fact] void should_parse_the_first_mapping() => _result.Mappings[0].ShouldEqual(new KeyValuePair<string, string>("first", "one"));
    [Fact] void should_trim_the_second_mapping() => _result.Mappings[1].ShouldEqual(new KeyValuePair<string, string>("second", "two"));
}
