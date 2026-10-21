// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.given;

public class namespace_bound_occurrences : Specification
{
    protected IChronicleConnection _connection;
    protected Contracts.Occurrences.IOccurrences _service;
    protected Occurrences _occurrences;
    protected EffectKey _key;
    protected Contracts.Occurrences.RecordEffectReceiptRequest _request;
    protected EffectReceiptResult _result;
    protected Exception _error;

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities>();
        ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(true);
        ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.EffectReceipts]);
        var services = Substitute.For<IServices>();
        _service = Substitute.For<Contracts.Occurrences.IOccurrences>();
        services.Occurrences.Returns(_service);
        ((IChronicleServicesAccessor)_connection).Services.Returns(services);
        var store = Substitute.For<IEventStore>();
        store.Connection.Returns(_connection);
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"tenant");
        _occurrences = new(store);
        _key = new("occurrence", "send", new("occurrence", 2));
        _service.GetEffectReceipt(Arg.Any<Contracts.Occurrences.GetEffectReceiptRequest>(), CallContext.Default)
            .Returns(QueryResult<Contracts.Occurrences.EffectReceiptRecordResponse?>.Success(Guid.NewGuid(), null));
        _service.RecordEffectReceipt(Arg.Any<Contracts.Occurrences.RecordEffectReceiptRequest>(), CallContext.Default).Returns(call =>
        {
            _request = call.Arg<Contracts.Occurrences.RecordEffectReceiptRequest>();
            return CommandResult<Contracts.Occurrences.RecordEffectReceiptResponse>.Success(Guid.NewGuid(), new()
            {
                Status = Contracts.Occurrences.EffectReceiptStatus.Recorded,
                Receipt = new()
                {
                    Id = _key.Value,
                    OccurrenceId = _key.Occurrence,
                    Epoch = _key.WrittenBy.Epoch,
                    Outcome = _request.Outcome,
                    Reasons = _request.Reasons,
                    Response = _request.EffectReceipt.Response,
                    RecordedAt = DateTimeOffset.UtcNow
                }
            });
        });
    }
}
