// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_the_namespace_notification_is_rejected : given.a_namespaces_grain
{
    Exception _error;

    void Establish() => _systemSequence.Append($"{_eventStore}/{_namespace}", new NamespaceAdded(_eventStore, _namespace))
        .Returns(AppendResult.Failed(CorrelationId.NotSet, new AppendError[] { new("append failed") }));

    async Task Because() => _error = await Catch.Exception(() => _namespaces.Ensure(_namespace));

    [Fact] void should_fail_the_ensure() => _error.ShouldBeOfExactType<NamespaceAddedCouldNotBeAppended>();
    [Fact] void should_not_record_the_namespace_as_created() => _state.NewNamespaces.ShouldBeEmpty();
    [Fact] void should_not_persist_the_namespace() => _silo.StorageManager.GetStorageStats(typeof(Namespaces).FullName)!.Writes.ShouldEqual(0);
    [Fact] async Task should_not_broadcast_the_namespace() => await _writer.DidNotReceive().Publish(Arg.Any<NamespaceAdded>());
}
