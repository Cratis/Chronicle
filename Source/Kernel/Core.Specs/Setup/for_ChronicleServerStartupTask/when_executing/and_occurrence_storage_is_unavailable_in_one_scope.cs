// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_occurrence_storage_is_unavailable_in_one_scope : given.a_startup_task
{
    readonly IOccurrenceQueue _queue = Substitute.For<IOccurrenceQueue>();
    readonly IOccurrencesStorage _failed = Substitute.For<IOccurrencesStorage>();

    void Establish()
    {
        var healthyName = new EventStoreNamespaceName("healthy");
        var healthy = Substitute.For<IEventStoreNamespaceStorage>();
        var occurrences = Substitute.For<IOccurrencesStorage>();
        _eventStoreStorage.Occurrences.Returns(_failed);
        _namespaceStorage.Occurrences.Returns(_failed);
        _namespaceStorage.HasData().Returns(Task.FromResult(false));
        _failed.GetPendingTriggers().Returns(Task.FromException<IEnumerable<TriggerId>>(new IOException("Simulated scope failure")));
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace, healthyName]));
        _eventStoreStorage.GetNamespace(healthyName).Returns(healthy);
        healthy.Occurrences.Returns(occurrences);
        occurrences.GetPendingTriggers().Returns(Task.FromResult<IEnumerable<TriggerId>>([new("trigger")]));
        OccurrenceQueueKey.Resolve(_grainFactory, _eventStore, healthyName.Value, "trigger").Returns(_queue);
    }

    Task Because() => Execute();
    [Fact] async Task should_attempt_both_application_and_namespace_recovery() => await _failed.Received(2).GetPendingTriggers();
    [Fact] async Task should_wake_work_in_the_healthy_namespace() => await _queue.Received(1).Wake();
    [Fact] void should_finish_startup() => _bootstrapClientsEnsured.ShouldBeTrue();
}
