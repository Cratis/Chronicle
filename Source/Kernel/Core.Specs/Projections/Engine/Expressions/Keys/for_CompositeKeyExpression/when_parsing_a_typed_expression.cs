// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpression;

public class when_parsing_a_typed_expression : Specification
{
    CompositeKeyExpression _result;

    void Because() => _result = CompositeKeyExpression.Parse("$composite( OrderKey , first = one , second=two )", (ProjectionId)"projection", PropertyPath.NotSet);

    [Fact] void should_keep_the_type() => _result.TypeName.ShouldEqual("OrderKey");
    [Fact] void should_parse_all_mappings() => _result.Mappings.Count.ShouldEqual(2);
    [Fact] void should_trim_the_first_mapping() => _result.Mappings[0].ShouldEqual(new KeyValuePair<string, string>("first", "one"));
}
