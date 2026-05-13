// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Specifications.for_ReadModels.when_getting_instances.with_limited_event_count.context;

namespace Cratis.Chronicle.Integration.Specifications.for_ReadModels.when_getting_instances;

[Collection(ChronicleCollection.Name)]
public class with_limited_event_count(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_with_events(chronicleFixture)
    {
        public IEnumerable<SomeReadModel> Results;

        async Task Because()
        {
            await AppendEvents();
            Results = await EventStore.ReadModels.GetInstances<SomeReadModel>(1);
        }
    }

    [Fact] void should_return_single_instance() => Context.Results.Count().ShouldEqual(1);
    [Fact] void should_have_number_from_first_event() => Context.Results.First().Number.ShouldEqual(Context.FirstEvent.Number);
    [Fact] void should_not_have_value_from_second_event() => Context.Results.First().Value.ShouldBeNull();
}
