// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Load.for_GrainCallLoadOutgoingFilter;

public class when_a_call_fails : Specification
{
    KernelLoad _load;
    Exception _exception;

    void Establish()
    {
        _load = new KernelLoad(Options.Create(new ChronicleOptions()), NullLogger<KernelLoad>.Instance, null, new ManualTimeProvider());
        var context = Substitute.For<IOutgoingGrainCallContext>();
        context.InterfaceMethod.Returns(typeof(IObserver).GetMethods(BindingFlags.Public | BindingFlags.Instance)[0]);
        context.TargetId.Returns(GrainId.Create("observer", "observer-id#the-store#the-namespace#event-log"));
        context.Invoke().Returns<Task>(_ => throw new InvalidOperationException("boom"));
        _context = context;
    }

    IOutgoingGrainCallContext _context;

    async Task Because() => _exception = await Catch.Exception(() => new GrainCallLoadOutgoingFilter(_load).Invoke(_context));

    [Fact] void should_let_the_failure_through() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_stop_counting_the_call() => _load.Report().Grains.Single().Pending.ShouldEqual(0);
}
