// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.Federation.for_FederationEnrollments;

public class when_reading_the_status_of_a_connected_kernel : Specification
{
    static readonly DateTimeOffset _exchange = new(2026, 3, 1, 10, 5, 0, TimeSpan.Zero);

    FederationEnrollments _enrollments;
    FederationEnrollmentStatus _connected;
    FederationEnrollmentStatus _neverConnected;
    FederationEnrollmentStatusResponse _response;

    void Establish()
    {
        _response = new()
        {
            Enrolled = true,
            Identity = "acme",
            Connected = true,
            LastExchange = _exchange,
            LastError = string.Empty
        };
        var wire = Substitute.For<IFederation>();
        wire.CurrentEnrollmentStatus(Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(_ => new QueryResult<FederationEnrollmentStatusResponse> { Data = _response });
        var services = Substitute.For<IServices>();
        services.Federation.Returns(wire);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        _enrollments = new(connection);
    }

    async Task Because()
    {
        _connected = await _enrollments.GetStatus();
        _response = new() { Enrolled = true, Identity = "acme", LastError = "The producer did not complete the pull" };
        _neverConnected = await _enrollments.GetStatus();
    }

    [Fact] void should_report_the_session_as_connected() => _connected.Connected.ShouldBeTrue();
    [Fact] void should_report_the_time_of_the_last_exchange() => _connected.LastExchange.ShouldEqual(_exchange);
    [Fact] void should_report_no_error_when_there_is_none() => _connected.LastError.ShouldEqual(string.Empty);
    [Fact] void should_report_not_connected_when_the_kernel_never_connected() => _neverConnected.Connected.ShouldBeFalse();
    [Fact] void should_report_no_exchange_when_the_kernel_never_connected() => _neverConnected.LastExchange.ShouldBeNull();
    [Fact] void should_report_the_last_error() => _neverConnected.LastError.ShouldEqual("The producer did not complete the pull");
}
