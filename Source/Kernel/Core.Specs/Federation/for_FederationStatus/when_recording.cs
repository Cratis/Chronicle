// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationStatus;

public class when_recording : Specification
{
    FederationStatus _status;
    FederationLinkStatus _link;
    FederationLinkStatus _unknownLink;
    FederationBindingStatus _binding;
    FederationBindingStatus _unknownBinding;

    void Establish() => _status = new(new FixedTimeProvider(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));

    void Because()
    {
        _status.RecordPull("link-1", 10, 1000);
        _status.RecordPull("link-1", 5, 500);
        _status.RecordThrottled("link-1");
        _status.SetBinding("orders", new("Streaming", 40, 100, null, null));
        _link = _status.GetLink("link-1");
        _unknownLink = _status.GetLink("other");
        _binding = _status.GetBinding("orders");
        _unknownBinding = _status.GetBinding("other");
    }

    [Fact] void should_accumulate_events() => _link.EventsDelivered.ShouldEqual(15UL);
    [Fact] void should_accumulate_bytes() => _link.BytesDelivered.ShouldEqual(1500UL);
    [Fact] void should_count_throttling() => _link.Throttled.ShouldEqual(1UL);
    [Fact] void should_record_the_last_pull_time() => _link.LastPull.ShouldEqual(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    [Fact] void should_report_zeros_for_an_unknown_link() => (_unknownLink.EventsDelivered + _unknownLink.Throttled).ShouldEqual(0UL);
    [Fact] void should_report_the_binding_state() => _binding.State.ShouldEqual("Streaming");
    [Fact] void should_compute_lag() => _binding.Lag.ShouldEqual(60UL);
    [Fact] void should_report_unknown_for_an_unknown_binding() => _unknownBinding.ShouldEqual(FederationBindingStatus.Unknown);

    sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
