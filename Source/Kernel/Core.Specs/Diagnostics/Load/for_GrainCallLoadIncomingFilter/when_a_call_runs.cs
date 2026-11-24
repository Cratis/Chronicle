// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Load.for_GrainCallLoadIncomingFilter;

public class when_a_call_runs : Specification
{
    readonly TaskCompletionSource _finish = new();
    KernelLoad _load;
    IIncomingGrainCallContext _context;
    int _runningWhileCalling;
    int _runningAfterwards;

    void Establish()
    {
        _load = new KernelLoad(Options.Create(new ChronicleOptions()), NullLogger<KernelLoad>.Instance, null, new ManualTimeProvider());
        _context = Substitute.For<IIncomingGrainCallContext>();
        _context.InterfaceMethod.Returns(typeof(IObserver).GetMethods(BindingFlags.Public | BindingFlags.Instance)[0]);
        _context.TargetId.Returns(GrainId.Create("observer", "observer-id#the-store#the-namespace#event-log"));
        _context.Invoke().Returns(_ => _finish.Task);
    }

    async Task Because()
    {
        var call = new GrainCallLoadIncomingFilter(_load).Invoke(_context);
        _runningWhileCalling = _load.Report().Grains.Single().Running;
        _finish.SetResult();
        await call;
        _runningAfterwards = _load.Report().Grains.Single().Running;
    }

    [Fact] void should_count_the_call_as_running_while_it_runs() => _runningWhileCalling.ShouldEqual(1);
    [Fact] void should_stop_counting_when_it_finishes() => _runningAfterwards.ShouldEqual(0);
}
