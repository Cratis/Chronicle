// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Transactions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_namespace_bound_append : a_receipted_append
{
    protected NamespaceBinding _binding;
    protected EventSequence _unbound;
    protected Contracts.Sequences.AppendToNamespaceRequest _boundSingle;
    protected Contracts.Sequences.AppendOnceToNamespaceRequest _boundOnce;
    protected Contracts.Sequences.AppendManyForEventSourcesToNamespaceRequest _boundBatch;
    protected Contracts.Sequences.AppendManyOnceToNamespaceRequest _boundKeyedBatch;

    void Establish()
    {
        ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.EffectReceipts, KernelCapabilities.IdempotentAppends]);
        _binding = new("store", "namespace", Guid.NewGuid(), NamespaceBindingStatus.Open, 1);
        _unbound = _eventSequence;
        _eventSequence = _unbound.BindNamespace(_binding);
        _sequences.AppendToNamespace(Arg.Any<Contracts.Sequences.AppendToNamespaceRequest>(), CallContext.Default).Returns(call =>
        {
            _boundSingle = call.Arg<Contracts.Sequences.AppendToNamespaceRequest>();
            return SingleResponse();
        });
        _sequences.AppendOnceToNamespace(Arg.Any<Contracts.Sequences.AppendOnceToNamespaceRequest>(), CallContext.Default).Returns(call =>
        {
            _boundOnce = call.Arg<Contracts.Sequences.AppendOnceToNamespaceRequest>();
            return CommandResult<Contracts.Sequences.AppendOnceResponse>.Success(Guid.NewGuid(), new()
            {
                SequenceNumber = 42,
                ConstraintViolations = [],
                Errors = []
            });
        });
        _sequences.AppendManyForEventSourcesToNamespace(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesToNamespaceRequest>(), CallContext.Default).Returns(call =>
        {
            _boundBatch = call.Arg<Contracts.Sequences.AppendManyForEventSourcesToNamespaceRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), _response);
        });
        _sequences.AppendManyOnceToNamespace(Arg.Any<Contracts.Sequences.AppendManyOnceToNamespaceRequest>(), CallContext.Default).Returns(call =>
        {
            _boundKeyedBatch = call.Arg<Contracts.Sequences.AppendManyOnceToNamespaceRequest>();
            return CommandResult<Contracts.Sequences.AppendManyOnceResponse>.Success(Guid.NewGuid(), new()
            {
                IsSuccess = true,
                SequenceNumbers = [42],
                ConstraintViolations = [],
                ConcurrencyViolations = [],
                Errors = []
            });
        });
    }

    protected UnitOfWork CreateUnitOfWork()
    {
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        store.GetEventSequence(EventSequenceId.Log).Returns(_unbound);
        return new(CorrelationId.New(), _ => { }, store);
    }

    protected void ShouldNotUseLegacyAppendRoutes() => _sequences.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name.StartsWith("Append", StringComparison.Ordinal))
        .All(call => call.GetMethodInfo().Name.EndsWith("ToNamespace", StringComparison.Ordinal)).ShouldBeTrue();

    static CommandResult<Contracts.Sequences.AppendResponse> SingleResponse() =>
        CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new()
        {
            SequenceNumber = 42,
            ConstraintViolations = [],
            Errors = []
        });
}
