// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_an_older_version_registers.and_the_newer_version_registered_first.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_an_older_version_registers;

/// <summary>
/// Covers https://github.com/Cratis/Chronicle/issues/5012: during a rolling deploy an old client pod that reconnects
/// registers its definitions after the new one did. The kernel must keep the definition the newer version registered.
/// </summary>
/// <param name="context">The context for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_newer_version_registered_first(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public IList<string> TagsAfterTheOlderRegistration;

        public override IEnumerable<Type> Projections => [typeof(TicketProjection)];
        public override IEnumerable<Type> EventTypes => [typeof(TicketOpened)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new TicketProjection());

        async Task Because()
        {
            await EventStore.Projections.GetHandlerFor<TicketProjection>().WaitTillSubscribed();

            var projections = ((IChronicleServicesAccessor)EventStore.Connection).Services.Projections;
            var definition = (await projections.GetAllDefinitions(new() { EventStore = EventStore.Name }))
                .Single(candidate => candidate.Identifier == typeof(TicketProjection).FullName);

            await Register(projections, definition, "99.0.0", "newer");
            await Register(projections, definition, "98.0.0", "older");

            TagsAfterTheOlderRegistration = (await projections.GetAllDefinitions(new() { EventStore = EventStore.Name }))
                .Single(candidate => candidate.Identifier == typeof(TicketProjection).FullName).Tags;
        }

        Task Register(Contracts.Projections.IProjections projections, ProjectionDefinition definition, string version, string tag)
        {
            definition.Tags = [tag];
            return projections.Register(new()
            {
                EventStore = EventStore.Name,
                Owner = ProjectionOwner.Client,
                Projections = [definition],
                Registrant = "the-app",
                Version = version
            });
        }
    }

    [Fact] void should_keep_the_definition_the_newer_version_registered() => Context.TagsAfterTheOlderRegistration.ShouldContainOnly("newer");
}
