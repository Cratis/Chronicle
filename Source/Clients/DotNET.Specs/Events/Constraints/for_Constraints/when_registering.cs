// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Events.Constraints;
using IContractsConstraints = Cratis.Chronicle.Contracts.Events.Constraints.IConstraints;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints;

public class when_registering : given.two_constraints
{
    IChronicleConnection _connection;
    IChronicleServicesAccessor _servicesAccessor;
    IServices _services;
    IContractsConstraints _constraintsService;
    Constraint _firstConstraintContract;
    Constraint _secondConstraintContract;
    RegisterConstraintsRequest _request;

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = _connection as IChronicleServicesAccessor;
        _services = Substitute.For<IServices>();
        _servicesAccessor.Services.Returns(_services);
        _eventStore.Connection.Returns(_connection);
        _constraintsService = Substitute.For<IContractsConstraints>();
        _services.Constraints.Returns(_constraintsService);
        _firstConstraintContract = new Constraint { Name = _firstConstraintName };
        _secondConstraintContract = new Constraint { Name = _secondConstraintName };
        _constraintsService
            .When(_ => _.Register(Arg.Any<RegisterConstraintsRequest>()))
            .Do(_ => _request = _.ArgAt<RegisterConstraintsRequest>(0));

        _constraints.Discover();
    }

    Task Because() => _constraints.Register();

    [Fact] void should_register_with_correct_event_store_name() => _request.EventStore.ShouldEqual(_eventStoreName.Value);
}
