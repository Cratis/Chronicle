// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class a_seeding_builder_with_discovered_globals : a_seeding_builder
{
    protected readonly List<SeedEventsRequest> _requests = [];
    internal GlobalSeedRegistration _registration;

    void Establish()
    {
        _registration = new();
        _seeding.UseGlobalSeedRegistration(_registration);
        _artifacts.EventSeeders.Returns([typeof(Seeds)]);
        _activator.Activate<ICanSeedEvents>(Arg.Any<IServiceProvider>(), typeof(Seeds))
            .Returns(new ActivatedArtifact<ICanSeedEvents>(new Seeds(), NullLogger<ActivatedArtifact>.Instance));
        _seedingService.When(_ => _.SeedEvents(Arg.Any<SeedEventsRequest>()))
            .Do(call => _requests.Add(call.Arg<SeedEventsRequest>()));
    }

    class Seeds : ICanSeedEvents
    {
        public void Seed(IEventSeedingBuilder builder)
        {
            builder.For("discovered", [new TestEvent("global")]);
            builder.ForNamespace("tenant").For("local", [new TestEvent("local")]);
        }
    }
}
