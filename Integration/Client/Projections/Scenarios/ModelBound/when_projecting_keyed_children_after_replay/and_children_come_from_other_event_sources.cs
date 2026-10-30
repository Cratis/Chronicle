// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_keyed_children_after_replay.and_children_come_from_other_event_sources.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_keyed_children_after_replay;

[Collection(ChronicleCollection.Name)]
public class and_children_come_from_other_event_sources(context context) : Given<context>(context)
{
    static readonly Guid[] _moduleIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
    static readonly ReplayChildrenFeatureAdded[] _features = Enumerable.Range(0, 9)
        .Select(index => new ReplayChildrenFeatureAdded(_moduleIds[index % _moduleIds.Length], Guid.NewGuid(), $"Feature {index}"))
        .ToArray();

    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public ReplayChildrenModule[] BeforeReplay = [];
        public ReplayChildrenModule[] Result = [];
        public IEnumerable<FailedPartition> FailedPartitions = [];

        IProjectionHandler _handler = default!;
        EventSequenceNumber _lastAppended = EventSequenceNumber.Unavailable;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(ReplayChildrenModuleAdded),
            typeof(ReplayChildrenFeatureAdded)
        ];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(ReplayChildrenModule)];

        async Task Establish()
        {
            var projectionId = EventStore.Projections.GetProjectionIdForModel<ReplayChildrenModule>();
            _handler = EventStore.Projections.GetAllHandlers().Single(candidate => candidate.Id == projectionId);
            await _handler.WaitTillSubscribed();

            var events = new List<EventForEventSourceId>();
            for (var index = 0; index < _features.Length; index++)
            {
                if (index < _moduleIds.Length)
                {
                    events.Add(new(_moduleIds[index], new ReplayChildrenModuleAdded($"Module {index}"), Causation.Unknown()));
                }

                var feature = _features[index];
                events.Add(new(feature.FeatureId, feature, Causation.Unknown()));
            }

            var appended = await EventStore.EventLog.AppendMany(events);
            _lastAppended = appended.SequenceNumbers.Last();
            await _handler.WaitTillReachesEventSequenceNumber(_lastAppended);
            BeforeReplay = await ReadModules();
        }

        async Task Because()
        {
            var replayJobId = await EventStore.Projections.Replay(_handler.Id);
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(replayJobId);
            await _handler.WaitTillSubscribed();
            await _handler.WaitTillActive();
            await _handler.WaitTillReachesEventSequenceNumber(_lastAppended);

            FailedPartitions = await _handler.GetFailedPartitions();
            Result = await ReadModules();
        }

        Task<ReplayChildrenModule[]> ReadModules() => Task.WhenAll(
            _moduleIds.Select(id => EventStore.ReadModels.GetInstanceById<ReplayChildrenModule>(id.ToString())));
    }

    [Fact] void should_keep_all_features_of_the_first_module() => FeaturesOf(_moduleIds[0]).ShouldContainOnly(ExpectedFeaturesOf(_moduleIds[0]));
    [Fact] void should_keep_all_features_of_the_second_module() => FeaturesOf(_moduleIds[1]).ShouldContainOnly(ExpectedFeaturesOf(_moduleIds[1]));
    [Fact] void should_keep_all_features_of_the_third_module() => FeaturesOf(_moduleIds[2]).ShouldContainOnly(ExpectedFeaturesOf(_moduleIds[2]));
    [Fact] void should_have_no_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_match_the_read_models_before_replay() => Assert.Equivalent(Context.BeforeReplay, Context.Result, strict: true);

    static IEnumerable<ReplayChildrenFeature> ExpectedFeaturesOf(Guid moduleId) => _features
        .Where(feature => feature.ModuleId == moduleId)
        .Select(feature => new ReplayChildrenFeature(feature.FeatureId, feature.Name));

    IEnumerable<ReplayChildrenFeature> FeaturesOf(Guid moduleId) => Context.Result.Single(module => module.Id == moduleId).Features;
}

[EventType]
public record ReplayChildrenModuleAdded(string Name);

[EventType]
public record ReplayChildrenFeatureAdded(Guid ModuleId, Guid FeatureId, string Name);

[FromEvent<ReplayChildrenModuleAdded>]
public record ReplayChildrenModule(
    Guid Id,
    string Name,
    [ChildrenFrom<ReplayChildrenFeatureAdded>(
        key: nameof(ReplayChildrenFeatureAdded.FeatureId),
        parentKey: nameof(ReplayChildrenFeatureAdded.ModuleId),
        identifiedBy: nameof(ReplayChildrenFeature.Id))]
    IEnumerable<ReplayChildrenFeature> Features);

public record ReplayChildrenFeature(Guid Id, string Name);

#pragma warning restore SA1402
