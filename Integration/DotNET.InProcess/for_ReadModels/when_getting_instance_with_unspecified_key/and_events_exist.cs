// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.InProcess.Integration.for_ReadModels.when_getting_instance_with_unspecified_key.and_events_exist.context;

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels.when_getting_instance_with_unspecified_key;

[Collection(ChronicleCollection.Name)]
public class and_events_exist(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_with_events(chronicleInProcessFixture)
    {
        public SomeReadModel Result;

        async Task Because()
        {
            await AppendEvents();
            Result = await EventStore.ReadModels.GetInstanceById<SomeReadModel>(ReadModelKey.Unspecified);
        }
    }

    [Fact] void should_have_number_from_first_event() => Context.Result.Number.ShouldEqual(Context.FirstEvent.Number);
    [Fact] void should_have_value_from_second_event() => Context.Result.Value.ShouldEqual(Context.SecondEvent.Value);
}
