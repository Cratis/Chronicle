// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// A namespace can be registered - and rediscovered on every server restart - long before anything is ever
/// written to it. Rehydrating jobs, reactors, pattern capture and event sequences for such a namespace would
/// materialize its underlying storage for no reason, so none of it should run until the namespace actually
/// holds data.
/// </summary>
public class and_the_namespace_has_no_data : given.a_startup_task
{
    void Establish() => _namespaceStorage.HasData().Returns(Task.FromResult(false));

    Task Because() => Execute();

    [Fact] void should_check_whether_the_namespace_has_data() => _namespaceStorage.Received(1).HasData();
    [Fact] void should_not_discover_and_register_reactors_for_the_namespace() => _reactors.DidNotReceive().DiscoverAndRegister(_eventStore, _namespace);
    [Fact] void should_not_subscribe_pattern_capture_for_the_namespace() => _patternCapture.DidNotReceive().Subscribe(_eventStore, _namespace);
    [Fact] void should_not_rehydrate_jobs() => _jobsManager.DidNotReceive().Rehydrate();
    [Fact] void should_not_rehydrate_event_sequences() => _eventSequences.DidNotReceive().Rehydrate();
    [Fact] void should_not_look_up_persisted_observers() => _observerStateStorage.DidNotReceive().GetAll();
}
