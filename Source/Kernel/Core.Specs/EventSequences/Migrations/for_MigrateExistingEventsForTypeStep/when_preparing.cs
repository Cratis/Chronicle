// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep;

public class when_preparing : given.the_job_step
{
    async Task Because() => await _jobStep.Prepare(_request);

    [Fact] void should_set_event_type_id_in_state() =>
        _stateStorage.State.EventTypeId.ShouldEqual(_eventTypeId);
}
