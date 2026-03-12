// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Observers;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Cli.for_ListObserversCommand.when_filtering_by_type;

public class and_type_is_reactor : Specification
{
    IEnumerable<ObserverInformation> _observers;
    IEnumerable<ObserverInformation> _result;

    void Establish() => _observers =
    [
        new ObserverInformation { Id = "1", Type = ObserverType.Reactor },
        new ObserverInformation { Id = "2", Type = ObserverType.Projection },
        new ObserverInformation { Id = "3", Type = ObserverType.Reactor },
    ];

    void Because() => _result = ListObserversCommand.FilterByType(_observers, "reactor");

    [Fact] void should_return_only_reactors() => _result.Count().ShouldEqual(2);
    [Fact] void should_contain_only_reactor_types() => _result.ShouldEachConformTo(o => o.Type == ObserverType.Reactor);
}
