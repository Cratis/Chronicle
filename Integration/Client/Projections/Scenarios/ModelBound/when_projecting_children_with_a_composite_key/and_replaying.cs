// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_children_with_a_composite_key.and_replaying.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_children_with_a_composite_key;

[Collection(ChronicleCollection.Name)]
public class and_replaying(context context) : Given<context>(context)
{
    static readonly PullRequestIdentity _updated = new("octo", "library");
    static readonly PullRequestIdentity _otherOwner = new("other", "library");
    static readonly PullRequestIdentity _otherRepository = new("octo", "tools");

    [Fact] void should_keep_exactly_one_child_per_key_before_replay() => Context.BeforeReplay.Select(item => item.Id).ShouldContainOnly([_updated, _otherOwner, _otherRepository]);
    [Fact] void should_keep_exactly_one_child_per_key_after_replay() => Context.AfterReplay.Select(item => item.Id).ShouldContainOnly([_updated, _otherOwner, _otherRepository]);
    [Fact] void should_replay_the_update_to_the_matching_child() => Context.AfterReplay.Single(item => item.Id == _updated).Title.ShouldEqual("Updated");
    [Fact] void should_remove_only_the_child_matching_both_parts() => Context.AfterRemoval.Select(item => item.Id).ShouldContainOnly([_otherOwner, _otherRepository]);

    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        static readonly Guid _dashboardId = Guid.Parse("3543b8d3-aa17-4405-9177-738a7fe608f7");

        public IReadOnlyList<TrackedPullRequest> BeforeReplay = [];
        public IReadOnlyList<TrackedPullRequest> AfterReplay = [];
        public IReadOnlyList<TrackedPullRequest> AfterRemoval = [];

        public override IEnumerable<Type> EventTypes => [typeof(DashboardOpened), typeof(PullRequestRecorded), typeof(PullRequestRenamed), typeof(PullRequestForgotten)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(PullRequestDashboard)];

        async Task Because()
        {
            var projectionId = EventStore.Projections.GetProjectionIdForModel<PullRequestDashboard>();
            var handler = EventStore.Projections.GetAllHandlers().Single(candidate => candidate.Id == projectionId);
            await handler.WaitTillSubscribed();
            var committed = await EventStore.EventLog.AppendMany(History());
            var last = committed.SequenceNumbers.Last();
            await handler.WaitTillReachesEventSequenceNumber(last);
            BeforeReplay = await ReadChildren();

            var replayJobId = await EventStore.Projections.Replay(projectionId);
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(replayJobId);
            await handler.WaitTillSubscribed();
            await handler.WaitTillActive();
            await handler.WaitTillReachesEventSequenceNumber(last);
            AfterReplay = await ReadChildren();

            var removal = await EventStore.EventLog.Append(_dashboardId, new PullRequestForgotten(_updated.Owner, _updated.Repository));
            await handler.WaitTillReachesEventSequenceNumber(removal.SequenceNumber);
            AfterRemoval = await ReadChildren();
        }

        static IEnumerable<EventForEventSourceId> History() =>
        [
            Wrap(new DashboardOpened()),
            Wrap(new PullRequestRecorded(_updated.Owner, _updated.Repository, "Initial")),
            Wrap(new PullRequestRecorded(_otherOwner.Owner, _otherOwner.Repository, "Other owner")),
            Wrap(new PullRequestRecorded(_otherRepository.Owner, _otherRepository.Repository, "Other repository")),
            Wrap(new PullRequestRenamed(_updated.Owner, _updated.Repository, "Updated"))
        ];

        static EventForEventSourceId Wrap(object @event) => new(_dashboardId, @event, Causation.Unknown());

        async Task<IReadOnlyList<TrackedPullRequest>> ReadChildren() =>
            (await EventStore.ReadModels.GetInstanceById<PullRequestDashboard>(_dashboardId.ToString())).PullRequests.ToArray();
    }

    [EventType]
    public record DashboardOpened;

    [EventType]
    public record PullRequestRecorded(string Owner, string Repository, string Title);

    [EventType]
    public record PullRequestRenamed(string Owner, string Repository, string Title);

    [EventType]
    public record PullRequestForgotten(string Owner, string Repository);

    public record PullRequestIdentity(string Owner, string Repository);

    public record TrackedPullRequest([Key] PullRequestIdentity Id, string Title);

    [FromEvent<DashboardOpened>]
    public record PullRequestDashboard(
        Guid Id,
        [ChildrenFrom<PullRequestRecorded>(CompositeKey = new[] { nameof(PullRequestRecorded.Owner), nameof(PullRequestRecorded.Repository) })]
        [ChildrenFrom<PullRequestRenamed>(CompositeKey = new[] { nameof(PullRequestRenamed.Owner), nameof(PullRequestRenamed.Repository) })]
        [RemovedWith<PullRequestForgotten>(CompositeKey = new[] { nameof(PullRequestForgotten.Owner), nameof(PullRequestForgotten.Repository) })]
        IEnumerable<TrackedPullRequest> PullRequests);
}
