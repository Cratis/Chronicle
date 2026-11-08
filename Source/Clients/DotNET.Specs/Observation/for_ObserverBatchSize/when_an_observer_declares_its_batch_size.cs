// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverBatchSize;

public class when_an_observer_declares_its_batch_size : Specification
{
    ObserverBatchSize _result;
    Contracts.Observation.PageDelivery _contract;

    void Because()
    {
        _result = ObserverBatchSize.For(typeof(Observer));
        _contract = _result.ToContract();
    }

    [Fact] void should_use_the_declared_events() => _result.MaxEvents.ShouldEqual(1000);
    [Fact] void should_use_the_declared_bytes() => _result.MaxBytes.ShouldEqual(65536);
    [Fact] void should_use_the_declared_delay() => _result.MaxDelay.ShouldEqual(TimeSpan.FromMilliseconds(50));
    [Fact] void should_use_the_declared_concurrency() => _result.MaxConcurrency.ShouldEqual(3);
    [Fact] void should_enable_page_delivery() => _contract.Enabled.ShouldBeTrue();
    [Fact] void should_send_the_events() => _contract.MaxEventsPerPage.ShouldEqual(1000U);
    [Fact] void should_send_the_bytes() => _contract.MaxBytesPerPage.ShouldEqual(65536U);
    [Fact] void should_send_the_delay() => _contract.MaxDelayMilliseconds.ShouldEqual(50U);

    [BatchSize(MaxEvents = 1000, MaxBytes = 65536, MaxDelayMilliseconds = 50, MaxConcurrency = 3)]
    public class Observer;
}
