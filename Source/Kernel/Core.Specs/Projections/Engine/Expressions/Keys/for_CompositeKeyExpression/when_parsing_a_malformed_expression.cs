// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpression;

public class when_parsing_a_malformed_expression : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => CompositeKeyExpression.Parse("$composite(OrderKey, first=one, invalid)", (ProjectionId)"projection", PropertyPath.NotSet));

    [Fact] void should_fail_on_the_invalid_component() => _result.ShouldBeOfExactType<InvalidCompositeKeyPropertyMappingExpression>();
}
