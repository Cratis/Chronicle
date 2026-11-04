// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;
using Cratis.Chronicle.Observation;

using context = Cratis.Chronicle.Integration.for_ReadModels.when_reading_a_pinned_projection_by_id.and_the_event_was_appended_at_a_later_generation.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_reading_a_pinned_projection_by_id;

[Collection(ChronicleCollection.Name)]
public class and_the_event_was_appended_at_a_later_generation(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : for_Reactors.when_generation_is_pinned.given.a_pinned_client(fixture)
    {
        public FirstGenerationPerson Result;
        public override IEnumerable<Type> EventTypes => [typeof(PinnedPersonRegisteredV1), typeof(PinnedPersonRegistered)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(PinnedPersonMigration)];
        public override IEnumerable<Type> Projections => [typeof(FirstGenerationPersonProjection)];

        async Task Because()
        {
            var handler = _pinnedStore.Projections.GetHandlerFor<FirstGenerationPersonProjection>();
            await handler.WaitTillSubscribed();
            var id = $"person-{Guid.NewGuid():N}";
            var append = await _pinnedStore.EventLog.Append(id, new PinnedPersonRegistered("Ada Lovelace"));
            (await append.WaitForCompletion()).IsSuccess.ShouldBeTrue();
            await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber);
            Result = await _pinnedStore.ReadModels.GetInstanceById<FirstGenerationPerson>(id);
        }
    }

    [Fact] void should_read_the_name_from_the_pinned_generation() => Context.Result.Name.ShouldEqual("Ada Lovelace");
}
