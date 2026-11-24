// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Load.for_GrainCallLoadOutgoingFilter;

public class when_a_call_is_made : Specification
{
    readonly TaskCompletionSource _answer = new();
    KernelLoad _load;
    GrainCallLoadOutgoingFilter _filter;
    IOutgoingGrainCallContext _context;
    Task _call;
    int _pendingWhileWaiting;
    int _pendingAfterwards;
    string _eventStore;

    void Establish()
    {
        _load = new KernelLoad(Options.Create(new ChronicleOptions()), NullLogger<KernelLoad>.Instance, null, new ManualTimeProvider());
        _filter = new GrainCallLoadOutgoingFilter(_load);
        _context = Substitute.For<IOutgoingGrainCallContext>();
        _context.InterfaceMethod.Returns(typeof(IObserver).GetMethods(BindingFlags.Public | BindingFlags.Instance)[0]);
        _context.TargetId.Returns(GrainId.Create("observer", "observer-id#the-store#the-namespace#event-log"));
        _context.Invoke().Returns(_ => _answer.Task);
    }

    async Task Because()
    {
        _call = _filter.Invoke(_context);
        _pendingWhileWaiting = _load.Report().Grains.Single().Pending;
        _eventStore = _load.Report().Grains.Single().EventStore;
        _answer.SetResult();
        await _call;
        _pendingAfterwards = _load.Report().Grains.Single().Pending;
    }

    [Fact] void should_count_the_call_as_pending_while_waiting() => _pendingWhileWaiting.ShouldEqual(1);
    [Fact] void should_name_the_event_store_of_the_grain() => _eventStore.ShouldEqual("the-store");
    [Fact] void should_stop_counting_once_it_is_answered() => _pendingAfterwards.ShouldEqual(0);
}
