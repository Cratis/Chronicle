// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.XUnit.Integration;
using KernelConcepts = Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Integration;

public class Specification<TChronicleFixture>(TChronicleFixture fixture) : Cratis.Chronicle.XUnit.Integration.Specification<TChronicleFixture>(fixture)
    where TChronicleFixture : IChronicleFixture
{
    public override bool AutoDiscoverArtifacts => false;

    public IEventStoreStorage EventStoreStorage =>
        Services.GetRequiredService<IStorage>().GetEventStore(Constants.EventStore);

    public IEventStoreNamespaceStorage GetEventStoreNamespaceStorage(KernelConcepts.EventStoreNamespaceName? namespaceName = null) =>
        EventStoreStorage.GetNamespace(namespaceName ?? KernelConcepts.EventStoreNamespaceName.Default);

    public IEventSequenceStorage GetEventLogStorage(KernelConcepts.EventStoreNamespaceName? namespaceName = null) =>
        GetEventStoreNamespaceStorage(namespaceName).GetEventSequence(KernelConcepts.EventSequences.EventSequenceId.Log);

    public IEventSequenceStorage GetSystemEventLogStorage(KernelConcepts.EventStoreNamespaceName? namespaceName = null) =>
        GetEventStoreNamespaceStorage(namespaceName).GetEventSequence(KernelConcepts.EventSequences.EventSequenceId.System);
}

public class Specification(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture);
