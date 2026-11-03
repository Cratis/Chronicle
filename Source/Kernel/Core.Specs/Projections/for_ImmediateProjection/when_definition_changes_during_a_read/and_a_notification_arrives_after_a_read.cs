// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_definition_changes_during_a_read;

public class and_a_notification_arrives_after_a_read : given.an_immediate_projection
{
    async Task Because()
    {
        await _grain.GetModelInstance();
        await _grain.OnProjectionDefinitionsChanged(CreateProjectionDefinition());
    }

    [Fact] void should_apply_the_change_immediately() => _grain.AppliedDefinitions.Count.ShouldEqual(1);
}
