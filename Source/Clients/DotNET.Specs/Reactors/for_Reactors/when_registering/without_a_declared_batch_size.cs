// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class without_a_declared_batch_size : given.all_dependencies
{
    PageDelivery _pageDelivery;

    void Establish()
    {
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReactorMessage>>().Take(1).Subscribe(message => _pageDelivery = message.Content.Value0!.PageDelivery);
            return Observable.Never<EventsToObserve>();
        });
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();
    }

    async Task Because() => await _reactors.Register();

    [Fact] void should_declare_page_support() => _pageDelivery.Enabled.ShouldBeTrue();
    [Fact] void should_send_the_per_event_default_of_max_events() => _pageDelivery.MaxEventsPerPage.ShouldEqual(100U);
    [Fact] void should_leave_max_bytes_to_the_kernel() => _pageDelivery.MaxBytesPerPage.ShouldEqual(0U);
    [Fact] void should_send_no_delay() => _pageDelivery.MaxDelayMilliseconds.ShouldEqual(0U);

    class MyReactor : IReactor;
}
