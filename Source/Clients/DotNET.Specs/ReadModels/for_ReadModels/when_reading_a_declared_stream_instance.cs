// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

public class when_reading_a_declared_stream_instance : given.a_stream_model
{
    GetInstanceByKeyRequest _request;
    MonthlyState _instance;

    void Establish()
    {
        var service = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(service);
        service.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>()).Returns(call =>
        {
            _request = call.Arg<GetInstanceByKeyRequest>();
            return new GetInstanceByKeyResponse { ReadModel = "{\"Id\":\"account\"}", StreamScoped = true, LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable };
        });
    }

    async Task Because() => _instance = await _readModels.GetInstanceById<MonthlyState>((EventSourceId)"account", (EventStreamId)"2026-10");

    [Fact] void should_read_the_stream_instance() => _instance.Id.ShouldEqual("account");
    [Fact] void should_use_the_source_as_the_key() => _request.ReadModelKey.ShouldEqual("account");
    [Fact] void should_send_the_source_type() => _request.EventSourceType.ShouldEqual("BankAccount");
    [Fact] void should_send_the_stream_type() => _request.EventStreamType.ShouldEqual("Transactions");
    [Fact] void should_send_the_stream_id() => _request.EventStreamId.ShouldEqual("2026-10");
    [Fact] void should_not_create_a_session() => _request.SessionId.ShouldEqual(string.Empty);
}
