// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_checking_has_event_source_id_for_tuple;

public class with_event_source_id_in_tuple : Specification
{
    ITuple _tuple;
    bool _result;

    void Establish() => _tuple = (EventSourceId.New(), "some-data", 42);

    void Because() => _result = _tuple.HasEventSourceId();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
