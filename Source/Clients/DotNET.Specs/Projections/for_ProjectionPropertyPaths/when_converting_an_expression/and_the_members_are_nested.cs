// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Projections.for_SetBuilder.given;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_converting_an_expression;

public class and_the_members_are_nested : Specification
{
    Expression<Func<NestedEvent, string>> _expression;
    PropertyPath _result;

    void Establish() => _expression = e => e.Brief.Topic;
    void Because() => _result = ProjectionPropertyPaths.FromExpression(_expression, new CamelCaseNamingPolicy(), ProjectionPathRole.Source);

    [Fact] void should_name_every_member() => _result.Path.ShouldEqual("brief.topic");
}
