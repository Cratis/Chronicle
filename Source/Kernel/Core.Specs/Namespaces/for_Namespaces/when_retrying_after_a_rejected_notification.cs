// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_retrying_after_a_rejected_notification : given.a_namespaces_grain
{
    async Task Establish()
    {
        _systemSequence.Append(_namespace.Value, new NamespaceAdded(_eventStore, _namespace))
            .Returns(AppendResult.Failed(CorrelationId.NotSet, new AppendError[] { new("append failed") }));
        await Catch.Exception(() => _namespaces.Ensure(_namespace));
        _systemSequence.Append(_namespace.Value, new NamespaceAdded(_eventStore, _namespace))
            .Returns(AppendResult.Success(CorrelationId.NotSet, Concepts.Events.EventSequenceNumber.First));
    }

    async Task Because() => await _namespaces.Ensure(_namespace);

    [Fact] async Task should_retry_the_durable_notification() =>
        await _systemSequence.Received(2).Append(_namespace.Value, new NamespaceAdded(_eventStore, _namespace));
    [Fact] void should_persist_the_namespace_once() => _silo.StorageManager.GetStorageStats(typeof(Namespaces).FullName)!.Writes.ShouldEqual(1);
    [Fact] async Task should_broadcast_the_namespace_once() => await _writer.Received(1).Publish(new NamespaceAdded(_eventStore, _namespace));
}
