// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_stopping_a_per_process_activation : given.lifecycle_occurrences
{
    async Task Establish()
    {
        _options.ActivationScope = ActivationScope.PerProcess;
        await _occurrences.Discover();
        await _occurrences.StartActivation(CancellationToken.None);
    }

    Task Because() => _occurrences.StopActivation(CancellationToken.None);

    [Fact] void should_record_shutdown_after_startup() => _recordings.Count.ShouldEqual(2);
    [Fact] void should_reuse_the_process_identity() => _recordings[1].ActivationId.ShouldEqual(_recordings[0].ActivationId);
    [Fact] void should_record_the_shutdown_kind() => _recordings[1].Kind.ShouldEqual(Contracts.Storage.Occurrences.OccurrenceKind.Shutdown);
    [Fact] void should_wait_for_application_scoped_delivery() => _observations.Single().ApplicationScope.ShouldBeTrue();
}
