// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_ending_an_activation_in_all_namespaces : given.lifecycle_occurrences
{
    async Task Establish()
    {
        _options.LifecycleScope = LifecycleScope.AllNamespaces;
        await _occurrences.Discover();
    }

    Task Because() => _occurrences.EndActivation(new("ended-rollout"));

    [Fact] void should_request_a_durable_namespace_snapshot() => _recordings.Single().AllNamespaces.ShouldBeTrue();
    [Fact] void should_name_the_ended_activation() => _recordings.Single().ActivationId.ShouldEqual("ended-rollout");
    [Fact] void should_record_shutdown() => _recordings.Single().Kind.ShouldEqual(Contracts.Storage.Occurrences.OccurrenceKind.Shutdown);
}
