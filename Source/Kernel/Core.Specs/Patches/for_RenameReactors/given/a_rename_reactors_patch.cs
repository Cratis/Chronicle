// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RenameReactors.given;

public class a_rename_reactors_patch : Specification
{
    protected RenameReactors _patch;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IReactorDefinitionsStorage _reactorStorage;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _reactorStorage = Substitute.For<IReactorDefinitionsStorage>();

        _storage.GetEventStore(EventStoreName.System).Returns(_eventStoreStorage);
        _eventStoreStorage.Reactors.Returns(_reactorStorage);

        _patch = new RenameReactors(_storage, Substitute.For<ILogger<RenameReactors>>());
    }
}
