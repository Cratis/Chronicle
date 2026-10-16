// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.given;

public class an_ensure_command : Specification
{
    protected static readonly EventStoreName _name = "the-store";
    protected IGrainFactory _grains;
    protected IStorage _storage;
    protected IEventTypes _eventTypes;
    protected IReactors _reactors;
    protected IKernelProjections _projections;
    protected INamespaces _namespaces;
    protected IEventSequence _systemSequence;
    protected EnsureEventStore _command;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventTypes = Substitute.For<IEventTypes>();
        _reactors = Substitute.For<IReactors>();
        _projections = Substitute.For<IKernelProjections>();
        _namespaces = Substitute.For<INamespaces>();
        _systemSequence = Substitute.For<IEventSequence>();
        _grains.GetGrain<INamespaces>(_name.Value).Returns(_namespaces);
        _grains.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_systemSequence);
        _command = new(_name);
    }

    protected Task Ensure() => _command.Handle(_grains, _storage, _eventTypes, _reactors, _projections);
}
