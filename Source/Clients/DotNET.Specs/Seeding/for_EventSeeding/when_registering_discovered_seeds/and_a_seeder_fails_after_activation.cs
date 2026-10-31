// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_a_seeder_fails_after_activation : given.a_seeding_builder_with_discovered_globals
{
    Exception _error;

    void Establish()
    {
        _artifacts.EventSeeders.Returns([.. _artifacts.EventSeeders, typeof(FailingSeeds)]);
        _activator.Activate<ICanSeedEvents>(Arg.Any<IServiceProvider>(), typeof(FailingSeeds))
            .Returns(new ActivatedArtifact<ICanSeedEvents>(new FailingSeeds(), NullLogger<ActivatedArtifact>.Instance));
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () =>
        {
            await _seeding.Discover();
            await _seeding.RegisterDiscovered();
        });
    }

    [Fact] void should_allow_namespaced_entries_to_register() => _error.ShouldBeNull();
    [Fact] void should_not_send_a_partial_global_set() => _requests.SelectMany(_ => _.GlobalByEventSource).ShouldBeEmpty();
    [Fact] void should_not_clear_the_previous_global_set() => _requests.Exists(_ => _.GlobalSetIsAuthoritative).ShouldBeFalse();
    [Fact] void should_send_the_successful_namespaced_entries() => _requests.SelectMany(_ => _.NamespacedEntries).Select(_ => _.Namespace).ShouldContainOnly(["tenant"]);

    class FailingSeeds : ICanSeedEvents
    {
        public void Seed(IEventSeedingBuilder builder)
        {
            builder.For("partial", [new TestEvent("global")]);
            throw new Exception("Seeder failed");
        }
    }
}
