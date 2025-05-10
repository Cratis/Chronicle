// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class all_dependencies : Specification
{
    protected IEventTypes _eventTypes;
    protected IEventSerializer _eventSerializer;
    protected IConstraints _constraints;
    protected ICausationManager _causationManager;
    protected IUnitOfWorkManager _unitOfWorkManager;
    protected IIdentityProvider _identityProvider;
    protected IChronicleConnection _connection;
    protected IEventSequences _eventSequences;
    protected IServices services;
    protected ICorrelationIdAccessor _correlationIdAccessor;


    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _constraints = Substitute.For<IConstraints>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _causationManager = Substitute.For<ICausationManager>();
        _unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        _identityProvider = Substitute.For<IIdentityProvider>();
        _connection = Substitute.For<IChronicleConnection>();
        _eventSequences = Substitute.For<IEventSequences>();
        services = Substitute.For<IServices>();
        _connection.Services.Returns(services);
        services.EventSequences.Returns(_eventSequences);
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        _correlationIdAccessor.Current.Returns((CorrelationId)Guid.Empty);
    }
}
