// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;

using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_generation_is_pinned.and_an_earlier_generation_has_personal_data.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_generation_is_pinned;

[Collection(ChronicleCollection.Name)]
public class and_an_earlier_generation_has_personal_data(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : for_Reactors.when_generation_is_pinned.given.a_pinned_client(fixture)
    {
        public PinnedProjectedPerson Live;
        public PinnedProjectedPerson Replayed;
        public override IEnumerable<Type> EventTypes => [typeof(PinnedPersonRegisteredV1), typeof(PinnedPersonRegistered)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(PinnedPersonMigration)];
        public override IEnumerable<Type> Projections => [typeof(PinnedPersonProjection)];

        async Task Because()
        {
            var handler = _pinnedStore.Projections.GetHandlerFor<PinnedPersonProjection>();
            await handler.WaitTillSubscribed();
            var id = $"person-{Guid.NewGuid():N}";
            var append = await _pinnedStore.EventLog.Append(id, new PinnedPersonRegisteredV1("Ada Lovelace"));
            (await append.WaitForCompletion()).IsSuccess.ShouldBeTrue();
            Live = await _pinnedStore.ReadModels.GetInstanceById<PinnedProjectedPerson>(id);
            var replay = await _pinnedStore.Projections.Replay<PinnedPersonProjection>();
            await _pinnedStore.Jobs.WaitTillJobCompletesOrIsDeleted(replay, TimeSpan.FromSeconds(30));
            await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber);
            Replayed = await _pinnedStore.ReadModels.GetInstanceById<PinnedProjectedPerson>(id);
        }
    }

    [Fact] void should_project_released_personal_data_live() => Context.Live.FullName.ShouldEqual("Ada Lovelace");
    [Fact] void should_project_the_same_plaintext_on_replay() => Context.Replayed.ShouldEqual(Context.Live);
}
