// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_ImportEventsStep;

public class when_performing_and_the_import_is_gone : given.an_import_events_step
{
    void Establish() => _record = null;

    async Task Because() => await Perform(CancellationToken.None);

    [Fact] void should_fail_the_step_so_the_job_fails_the_import() => _result.TryGetException(out _).ShouldBeTrue();
}
