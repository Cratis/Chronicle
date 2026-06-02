// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;
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
    internal IChronicleServicesAccessor _serviceAccessor;
    internal IEventSequences _eventSequences;
    internal IServices services;
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected IConcurrencyScopeStrategies _concurrencyScopeStrategies;
    protected IConcurrencyScopeStrategy _concurrencyScopeStrategy;
    protected IImmutableList<Causation> _currentCausationChain;
    protected ConcurrencyScope _defaultConcurrencyScope;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _constraints = Substitute.For<IConstraints>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _causationManager = Substitute.For<ICausationManager>();
        _unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        _identityProvider = Substitute.For<IIdentityProvider>();
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _serviceAccessor = _connection as IChronicleServicesAccessor;
        _eventSequences = Substitute.For<IEventSequences>();
        services = Substitute.For<IServices>();
        _serviceAccessor.Services.Returns(services);
        services.EventSequences.Returns(_eventSequences);
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        _correlationIdAccessor.Current.Returns((CorrelationId)Guid.Empty);
        _concurrencyScopeStrategies = Substitute.For<IConcurrencyScopeStrategies>();
        _concurrencyScopeStrategy = Substitute.For<IConcurrencyScopeStrategy>();
        _currentCausationChain = [Causation.Unknown()];
        _defaultConcurrencyScope = new(42UL);
        _causationManager.GetCurrentChain().Returns(_currentCausationChain);
        _concurrencyScopeStrategies.GetFor(Arg.Any<IEventSequence>()).Returns(_concurrencyScopeStrategy);
        _concurrencyScopeStrategy.GetScope(
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<IEnumerable<EventType>?>()).Returns(Task.FromResult(_defaultConcurrencyScope));
    }
}
