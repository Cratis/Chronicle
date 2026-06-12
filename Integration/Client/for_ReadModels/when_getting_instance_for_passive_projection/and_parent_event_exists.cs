// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.and_parent_event_exists.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection;

[Collection(ChronicleCollection.Name)]
public class and_parent_event_exists(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid ApplicationId;
        public PassiveApplication Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(PassiveApplicationCreated),
            typeof(PassiveEventModelCreated)
        ];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(PassiveApplication)];

        async Task Because()
        {
            ApplicationId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-555555555555");

            // A passive projection never subscribes an observer, so nothing is ever written to a
            // materialized sink. Appending the parent event and immediately resolving the instance by
            // key must produce the read model via on-demand immediate projection — not return null.
            await EventStore.EventLog.Append(ApplicationId.ToString(), new PassiveApplicationCreated("Sample Application"));

            Result = await EventStore.ReadModels.GetInstanceById<PassiveApplication>(ApplicationId.ToString());
        }
    }

    [Fact] void should_return_the_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_application_name() => Context.Result.Name.ShouldEqual("Sample Application");
    [Fact] void should_not_have_event_models_yet() => Context.Result.EventModels.ShouldBeNull();
}

[EventType]
public record PassiveApplicationCreated(string Name);

[EventType]
public record PassiveEventModelCreated(Guid ApplicationId, string Name);

[Passive]
[FromEvent<PassiveApplicationCreated>]
public record PassiveApplication(
    Guid Id,
    string Name,
    [ChildrenFrom<PassiveEventModelCreated>(parentKey: nameof(PassiveEventModelCreated.ApplicationId))]
    IEnumerable<PassiveEventModel> EventModels);

[FromEvent<PassiveEventModelCreated>]
public record PassiveEventModel(Guid Id, string Name);

#pragma warning restore SA1402
