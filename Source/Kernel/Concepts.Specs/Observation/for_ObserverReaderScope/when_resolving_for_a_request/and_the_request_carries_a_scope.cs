// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverReaderScope.when_resolving_for_a_request;

public class and_the_request_carries_a_scope : Specification
{
    static readonly EventType _first = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    static readonly EventType _second = new("22222222-2222-2222-2222-222222222222", EventTypeGeneration.First);
    ObserverReaderScope _carried;
    ObserverReaderScope _result;

    void Establish() => _carried = ObserverReaderScope.Explicit([_first]);

    void Because() => _result = ObserverReaderScope.ForRequest(_carried, [_second]);

    [Fact] void should_use_the_carried_scope() => _result.ShouldBeSame(_carried);
}
