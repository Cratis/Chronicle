// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.for_Defaults.given;

public class store_identity : Specification
{
    protected Defaults _defaults;
    protected readonly EventStoreName _name = new("scenario-store");
    protected readonly EventStoreNamespaceName _namespace = new("scenario-namespace");
    protected readonly EventSourceId _source = new("source");
    protected readonly InMemoryClientArtifactsProvider _artifacts = new() { EventTypes = [typeof(DefaultsIdentityRecorded)] };

    void Destroy() => ((IDisposable)_defaults.EventStore).Dispose();

    [EventType]
    public record DefaultsIdentityRecorded(string Value);
}
