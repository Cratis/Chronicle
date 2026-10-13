// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.given;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_converting_an_expression;

public class and_a_target_calls_a_derived_function : Specification
{
    Expression<Func<CalendarModel, object>> _expression;
    Exception _error;

    void Establish() => _expression = model => model.Occurred.Week();
    void Because() => _error = Catch.Exception(() => ProjectionPropertyPaths.FromExpression(_expression, new CamelCaseNamingPolicy(), ProjectionPathRole.Target, "the target week"));

    [Fact] void should_reject_a_function_on_the_target() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
}
