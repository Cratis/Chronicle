// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.given;

public class no_constraints : Specification
{
    protected EventStoreName _eventStoreName = "SomeEventStore";
    protected IEventStore _eventStore;
    protected IChronicleConnection _connection;
    internal IChronicleServicesAccessor _servicesAccessor;
    internal IServices _services;

    protected IEnumerable<ICanProvideConstraints> _constraintsProviders;
    protected ICanProvideConstraints _constraintsProvider;
    protected Constraints _constraints;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns(_eventStoreName);
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = _connection as IChronicleServicesAccessor;
        _services = Substitute.For<IServices>();
        _servicesAccessor.Services.Returns(_services);
        _eventStore.Connection.Returns(_connection);

        _constraintsProvider = Substitute.For<ICanProvideConstraints>();
        _constraintsProviders = [_constraintsProvider];
        _constraints = new Constraints(_eventStore, _constraintsProviders);
    }
}
