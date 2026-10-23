// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Options;
using context = Cratis.Chronicle.Kernel.Integration.Projections.when_redacting_an_event.and_projection_is_watermarked.context;

namespace Cratis.Chronicle.Kernel.Integration.Projections.when_redacting_an_event;

[Collection(ChronicleCollection.Name)]
public class and_projection_is_watermarked(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        EventSequenceNumber _redactedSequenceNumber;

        public const string Source = "redacted-projection";
        public ContentReadModel AfterRedaction;
        protected virtual bool UseDefaultKey => false;

        public override bool AutoDiscoverArtifacts => false;
        public override IEnumerable<Type> EventTypes => [typeof(ContentChanged)];
        public override IEnumerable<Type> Projections => [typeof(ContentProjection)];

        protected async Task Establish()
        {
            Services.GetRequiredService<IOptions<Configuration.ChronicleOptions>>().Value.ReadModels.GuardSinkWritesOnWatermark.ShouldBeTrue();
            await EventStore.DiscoverAll();
            await EventStore.RegisterAll();
            (await EventStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
            var projection = EventStore.Projections.GetHandlerFor<ContentProjection>();
            await ConfigureKey(projection.Id.Value);
            await projection.WaitTillActive();
            await EventStore.EventLog.Append(Source, new ContentChanged("retained content"));
            var appended = await EventStore.EventLog.Append(Source, new ContentChanged("redacted content"));
            _redactedSequenceNumber = appended.SequenceNumber;
            await projection.WaitTillReachesEventSequenceNumber(appended.SequenceNumber);
            var before = await EventStore.ReadModels.GetInstanceById<ContentReadModel>(Source);
            before.Content.ShouldEqual("redacted content");
            await EventStore.Jobs.WaitForThereToBeNoJobs(TimeSpan.FromSeconds(30));
        }

        async Task ConfigureKey(string projectionId)
        {
            if (UseDefaultKey)
            {
                var manager = Services.GetRequiredService<IGrainFactory>().GetGrain<Cratis.Chronicle.Projections.IProjectionsManager>(EventStore.Name.Value);
                var definition = (await manager.GetProjectionDefinitions()).Single(definition => definition.Identifier.Value == projectionId);
                await manager.Register([definition with
                {
                    From = definition.From.ToDictionary(pair => pair.Key, pair => pair.Value with { Key = new("") })
                }]);
            }
            var engine = await Services.GetRequiredService<Cratis.Chronicle.Projections.Engine.IProjectionsManager>()
                .GetCurrent(EventStore.Name.Value, EventStore.Namespace.Value, projectionId);
            engine.ShouldNotBeNull();
            engine.IsEventSourceKeyed.ShouldEqual(UseDefaultKey);
        }

        protected async Task Because()
        {
            // The fixture awaits the kernel's mutation and replay admission before returning.
            await this.RedactEvent(_redactedSequenceNumber, "redacted by specification");
            await EventStore.Jobs.WaitForThereToBeNoJobs(TimeSpan.FromSeconds(30));
            var projection = EventStore.Projections.GetHandlerFor<ContentProjection>();
            var observerKey = new Concepts.Observation.ObserverKey(projection.Id.Value, EventStore.Name.Value, EventStore.Namespace.Value, Concepts.EventSequences.EventSequenceId.Log);
            var state = await Services.GetRequiredService<IGrainFactory>().GetGrain<Observation.IObserver>(observerKey).GetState();
            state.ReplayingPartitions.ShouldBeEmpty();
            (await projection.GetFailedPartitions()).ShouldBeEmpty();
            var events = await EventStore.EventLog.GetFromSequenceNumber(_redactedSequenceNumber);
            events.Single(@event => @event.Context.SequenceNumber == _redactedSequenceNumber).Context.EventType.Id.Value.ShouldEqual("EventRedacted");
            AfterRedaction = await EventStore.ReadModels.GetInstanceById<ContentReadModel>(Source);
        }
    }

    [Fact] void should_rebuild_without_the_redacted_content() => Context.AfterRedaction.Content.ShouldEqual("retained content");
}
