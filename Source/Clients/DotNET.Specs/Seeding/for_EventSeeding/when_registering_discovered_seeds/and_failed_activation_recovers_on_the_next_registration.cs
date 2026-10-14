// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_failed_activation_recovers_on_the_next_registration : given.an_incomplete_discovery
{
    async Task Establish()
    {
        FailActivation();
        await _seeding.Discover();
        await _seeding.RegisterDiscovered();
        _requests.Clear();
        _activator.Activate<ICanSeedEvents>(Arg.Any<IServiceProvider>(), typeof(UnavailableSeeds))
            .Returns(new ActivatedArtifact<ICanSeedEvents>(new UnavailableSeeds(), NullLogger<ActivatedArtifact>.Instance));
    }

    Task Because() => _seeding.RegisterDiscovered();

    [Fact] void should_retry_activation() => _activator.Received(2).Activate<ICanSeedEvents>(Arg.Any<IServiceProvider>(), typeof(UnavailableSeeds));
    [Fact] void should_declare_the_complete_recovered_set() => _requests.Single(_ => _.GlobalSetIsAuthoritative).GlobalByEventSource.Select(_ => _.EventSourceId).ShouldContainOnly(["discovered", "unavailable"]);
}
