// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_nested_identities.and_a_child_is_briefed_again.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_nested_identities;

[Collection(ChronicleCollection.Name)]
public class and_a_child_is_briefed_again(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid FirstId;
        public Guid SecondId;
        public NestedIdentityReadModel Result;
        public IEnumerable<FailedPartition> FailedPartitions = [];

        public override IEnumerable<Type> EventTypes => [typeof(NestedIdentityBriefRecorded)];
        public override IEnumerable<Type> Projections => [typeof(NestedIdentityProjection)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new NestedIdentityProjection());

        void Establish()
        {
            FirstId = Guid.NewGuid();
            SecondId = Guid.NewGuid();
        }

        async Task Because()
        {
            var projection = EventStore.Projections.GetHandlerFor<NestedIdentityProjection>();
            await projection.WaitTillSubscribed();
            var parentId = EventSourceId.New();
            await EventStore.EventLog.Append(parentId, new NestedIdentityBriefRecorded(new(FirstId, "First")));
            await EventStore.EventLog.Append(parentId, new NestedIdentityBriefRecorded(new(SecondId, "Second")));
            var result = await EventStore.EventLog.Append(parentId, new NestedIdentityBriefRecorded(new(FirstId, "Updated")));
            await projection.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
            FailedPartitions = await projection.GetFailedPartitions();
            Result = await EventStore.ReadModels.GetInstanceById<NestedIdentityReadModel>(parentId);
        }
    }

    [Fact] void should_have_no_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_keep_two_children() => Context.Result.Children.Count().ShouldEqual(2);
    [Fact] void should_update_the_existing_child() => Context.Result.Children.Single(child => child.Identity.Id == Context.FirstId).Identity.Topic.ShouldEqual("Updated");
    [Fact] void should_preserve_the_other_child() => Context.Result.Children.Single(child => child.Identity.Id == Context.SecondId).Identity.Topic.ShouldEqual("Second");
}

/// <summary>
/// The identity and topic of a recorded brief.
/// </summary>
/// <param name="Id">The brief identifier.</param>
/// <param name="Topic">The brief topic.</param>
public record NestedIdentityBrief(Guid Id, string Topic);

/// <summary>
/// A brief was recorded for a child of the event source.
/// </summary>
/// <param name="Brief">The recorded brief.</param>
[EventType]
public record NestedIdentityBriefRecorded(NestedIdentityBrief Brief);

/// <summary>
/// A child identified by a nested member.
/// </summary>
/// <param name="Identity">The child's identity and current topic.</param>
public record NestedIdentityChild(NestedIdentityBrief Identity);

/// <summary>
/// The children briefed for an event source.
/// </summary>
/// <param name="Children">The briefed children.</param>
public record NestedIdentityReadModel(IEnumerable<NestedIdentityChild> Children);

/// <summary>
/// Projects briefs into children with nested identities.
/// </summary>
public class NestedIdentityProjection : IProjectionFor<NestedIdentityReadModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<NestedIdentityReadModel> builder) => builder
        .NoAutoMap()
        .Children(model => model.Children, children => children
            .IdentifiedBy(child => child.Identity.Id)
            .From<NestedIdentityBriefRecorded>(from => from
                .UsingKey(e => e.Brief.Id)
                .UsingParentKeyFromContext(context => context.EventSourceId)
                .Set(child => child.Identity.Id).To(e => e.Brief.Id)
                .Set(child => child.Identity.Topic).To(e => e.Brief.Topic)));
}
