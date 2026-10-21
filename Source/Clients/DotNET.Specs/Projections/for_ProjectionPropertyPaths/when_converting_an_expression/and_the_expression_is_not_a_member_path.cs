// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Projections.for_SetBuilder.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_converting_an_expression;

public class and_the_expression_is_not_a_member_path : Specification
{
    Expression<Func<NestedEvent, string>> _expression;
    Exception _result;

    void Establish() => _expression = e => e.Brief.Topic.ToUpperInvariant();
    void Because() => _result = Catch.Exception(() => ProjectionPropertyPaths.FromExpression(_expression, new CamelCaseNamingPolicy(), ProjectionPathRole.Source, "the event topic"));

    [Fact] void should_reject_the_expression() => _result.ShouldBeOfExactType<InvalidPropertyExpression>();
}
