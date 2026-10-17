// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.given;

public class store_identity : Specification
{
    protected EventStoreForTesting _store;
    protected readonly EventStoreName _name = new("scenario-store");
    protected readonly EventStoreNamespaceName _namespace = new("scenario-namespace");
    protected readonly EventSourceId _source = new("source");
    protected readonly InMemoryClientArtifactsProvider _artifacts = new() { EventTypes = [typeof(StoreIdentityRecorded)] };

    void Destroy() => _store.Dispose();

    [EventType]
    public record StoreIdentityRecorded(string Value);
}
