// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class a_legacy_join_and_an_inherited_join : given.a_projection_definition_comparer
{
    ProjectionDefinitionCompareResult _result;

    void Establish() => _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);

    async Task Because()
    {
        var legacy = CreateDefinition(join: new Dictionary<EventType, JoinDefinition>
        {
            [(EventType)"Joined"] = new((PropertyPath)"joinId", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet)
        });
        var current = CreateDefinition(join: new Dictionary<EventType, JoinDefinition>
        {
            [(EventType)"Joined"] = new((PropertyPath)"joinId", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, AutoMap.Inherit)
        });
        _result = await _comparer.Compare(_projectionKey, legacy, current);
    }

    [Fact] void should_not_trigger_a_replay() => _result.ShouldEqual(ProjectionDefinitionCompareResult.Same);
}
