// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationStatus;

public class when_recording_the_dialer_session : Specification
{
    static readonly DateTimeOffset _now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    FederationStatus _status;
    FederationDialStatus _initial;
    FederationDialStatus _afterRenewal;
    FederationDialStatus _afterExchange;
    FederationDialStatus _afterClose;

    void Establish() => _status = new(new FixedTimeProvider(_now));

    void Because()
    {
        _initial = _status.GetDial();
        _status.RecordDialRenewed();
        _afterRenewal = _status.GetDial();
        _status.RecordDialExchange();
        _afterExchange = _status.GetDial();
        _status.RecordDialClosed();
        _afterClose = _status.GetDial();
    }

    [Fact] void should_start_not_connected_without_an_exchange() => _initial.ShouldEqual(FederationDialStatus.None);
    [Fact] void should_count_a_renewal_as_an_exchange_without_connecting() => (_afterRenewal.Connected, _afterRenewal.LastExchange).ShouldEqual((false, (DateTimeOffset?)_now));
    [Fact] void should_connect_on_an_exchange() => (_afterExchange.Connected, _afterExchange.LastExchange).ShouldEqual((true, (DateTimeOffset?)_now));
    [Fact] void should_disconnect_but_keep_the_exchange_when_closed() => (_afterClose.Connected, _afterClose.LastExchange).ShouldEqual((false, (DateTimeOffset?)_now));

    sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
