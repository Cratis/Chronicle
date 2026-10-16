// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_stopping_one_logical_instance : given.lifecycle_occurrences
{
    async Task Establish()
    {
        await _occurrences.Discover();
        await _occurrences.StartActivation(CancellationToken.None);
    }

    Task Because() => _occurrences.StopActivation(CancellationToken.None);

    [Fact] void should_record_startup_only() => _recordings.Count.ShouldEqual(1);
    [Fact] void should_use_the_host_activation() => _recordings[0].ActivationId.ShouldEqual("rollout");
    [Fact] void should_record_startup_at_application_scope() => _recordings[0].AllNamespaces.ShouldBeFalse();
    [Fact] void should_not_turn_scale_in_into_shutdown() => _recordings[0].Kind.ShouldEqual(Contracts.Storage.Occurrences.OccurrenceKind.Startup);
}
