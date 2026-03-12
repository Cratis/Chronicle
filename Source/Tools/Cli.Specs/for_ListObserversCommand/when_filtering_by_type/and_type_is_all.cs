// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Cli.Commands.Observers;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Cli.for_ListObserversCommand.when_filtering_by_type;

public class and_type_is_all : Specification
{
    IEnumerable<ObserverInformation> _observers;
    IEnumerable<ObserverInformation> _result;

    void Establish() => _observers =
    [
        new ObserverInformation { Id = "1", Type = ObserverType.Reactor },
        new ObserverInformation { Id = "2", Type = ObserverType.Projection },
    ];

    void Because() => _result = ListObserversCommand.FilterByType(_observers, "all");

    [Fact] void should_return_all_observers() => _result.Count().ShouldEqual(2);
}
