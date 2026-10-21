// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_all_events_are_rejected : given.a_passive_reducer
{
    void Establish() => Seed(Event(1, 100, tags: ["red"]));

    async Task Because()
    {
        await ReadById();
        await ReadAll();
    }

    [Fact] void should_leave_the_instance_absent() => _instance.ShouldBeNull();
    [Fact] void should_return_no_collection_instances() => _instances.ShouldBeEmpty();
    [Fact] void should_not_activate_a_reducer() => _artifactActivator.DidNotReceive().Activate(Arg.Any<IServiceProvider>(), ReducerType);
}
