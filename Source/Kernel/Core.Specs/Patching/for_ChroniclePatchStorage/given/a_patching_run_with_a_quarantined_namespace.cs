// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.given;

/// <summary>
/// A real <see cref="PatchManager"/> with a real fan-out patch over one event store that is not quarantined, but one
/// of whose namespaces is. The namespaces grain drops a quarantined namespace from what it reports, as the real one does.
/// </summary>
public class a_patching_run_with_a_quarantined_namespace : a_patching_run
{
    protected static readonly EventStoreName Store = "some-store";
    protected static readonly EventStoreNamespaceName Open = "open-namespace";
    protected static readonly EventStoreNamespaceName Held = "held-namespace";

    protected ResourceNameSnapshot _resourceNames;
    protected IEventStoreStorage _eventStoreStorage;

    void Establish()
    {
        _resourceNames = new ResourceNameSnapshot();
        _resourceNames.Publish(new ResourceNameAuditResult([], [new ResourceOwner(Store.Value, Held.Value)], []));

        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<INamespaceStorage>();
        namespaceStorage.GetAll().Returns(Task.FromResult<IEnumerable<NamespaceState>>([new NamespaceState(Open, DateTimeOffset.UtcNow), new NamespaceState(Held, DateTimeOffset.UtcNow)]));
        _eventStoreStorage.Namespaces.Returns(namespaceStorage);
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([Store]));
        _storage.GetEventStore(Store).Returns(_eventStoreStorage);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns(_ => Task.FromResult<IEnumerable<EventStoreNamespaceName>>([.. new[] { Open, Held }.Where(name => !_resourceNames.IsQuarantined(Store, name))]));
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<INamespaces>(Store).Returns(namespaces);

        _patches.Clear();
        _patches.Add(new RemoveUnhashedUniqueConstraintValues(_storage, grains, NullLogger<RemoveUnhashedUniqueConstraintValues>.Instance, _resourceNames));
    }
}
