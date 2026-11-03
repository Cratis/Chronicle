// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.given;

namespace Cratis.Chronicle.Projections.for_Projection.when_replaying;

public class and_an_existing_container_is_reused : a_rebuild_with_erased_subjects
{
    async Task Establish()
    {
        foreach (var (id, state) in _old.Documents) _destination.Documents[id] = state;
        await CreateProjection();
    }

    Task Because() => ReplayProjection();

    [Fact] void should_leave_the_unrelated_orphan_in_the_dirty_destination() => _destination.Documents.ContainsKey("orphan").ShouldBeTrue();
    [Fact] void should_not_scrub_a_property_outside_the_emitted_changes() => _stored["obsolete"]!.GetValue<string>().ShouldEqual("no longer mapped");
}
