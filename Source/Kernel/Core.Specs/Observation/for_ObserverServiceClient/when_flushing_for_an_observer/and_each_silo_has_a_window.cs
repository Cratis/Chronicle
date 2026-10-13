// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverServiceClient.when_flushing_for_an_observer;

public class and_each_silo_has_a_window : Specification
{
    readonly Guid _firstWindow = Guid.NewGuid();
    readonly Guid _secondWindow = Guid.NewGuid();
    readonly SiloAddress _firstAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
    readonly SiloAddress _secondAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
    readonly ObserverDetails _details = new(new("observer", "store", "namespace", "event-log"), ObserverType.Projection);
    Dictionary<SiloAddress, IObserverService> _silos;
    IReadOnlyDictionary<SiloAddress, Guid[]> _result;

    void Establish()
    {
        var first = Substitute.For<IObserverService>();
        var second = Substitute.For<IObserverService>();
        first.FlushFor(_details, true).Returns(Task.FromResult<Guid[]>([_firstWindow]));
        second.FlushFor(_details, true).Returns(Task.FromResult<Guid[]>([_secondWindow]));
        _silos = new() { [_firstAddress] = first, [_secondAddress] = second };
    }

    async Task Because() => _result = await ObserverServiceClient.FlushSilosFor(_silos, _details, true);

    [Fact] void should_collect_the_first_silos_window() => _result[_firstAddress].ShouldContainOnly(_firstWindow);
    [Fact] void should_collect_the_second_silos_window() => _result[_secondAddress].ShouldContainOnly(_secondWindow);
}
