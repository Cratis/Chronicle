// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationStatus;

public class when_recording_inbound_and_sessions : Specification
{
    FederationStatus _status;
    FederationInboundStatus _inbound;
    FederationInboundStatus _unknownInbound;
    bool _openAfterTwoOpened;
    bool _openAfterOneClosed;
    bool _openAfterAllClosed;
    bool _unknownSession;

    void Establish() => _status = new(new FixedTimeProvider(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));

    void Because()
    {
        _status.RecordInbound("hosted-acme", 10, 1000);
        _status.RecordInbound("hosted-acme", 5, 500);
        _inbound = _status.GetInbound("hosted-acme");
        _unknownInbound = _status.GetInbound("other");

        _status.RecordSessionOpened("acme");
        _status.RecordSessionOpened("acme");
        _openAfterTwoOpened = _status.HasSession("acme");
        _status.RecordSessionClosed("acme");
        _openAfterOneClosed = _status.HasSession("acme");
        _status.RecordSessionClosed("acme");
        _status.RecordSessionClosed("acme");
        _openAfterAllClosed = _status.HasSession("acme");
        _unknownSession = _status.HasSession("other");
    }

    [Fact] void should_accumulate_inbound_events() => _inbound.EventsReceived.ShouldEqual(15UL);
    [Fact] void should_accumulate_inbound_bytes() => _inbound.BytesReceived.ShouldEqual(1500UL);
    [Fact] void should_record_the_last_received_time() => _inbound.LastReceived.ShouldEqual(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    [Fact] void should_report_zeros_for_unknown_inbound() => (_unknownInbound.EventsReceived + _unknownInbound.BytesReceived).ShouldEqual(0UL);
    [Fact] void should_report_connected_while_a_session_is_open() => (_openAfterTwoOpened, _openAfterOneClosed).ShouldEqual((true, true));
    [Fact] void should_report_not_connected_when_all_sessions_ended() => _openAfterAllClosed.ShouldBeFalse();
    [Fact] void should_report_not_connected_for_an_unknown_subject() => _unknownSession.ShouldBeFalse();

    sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
