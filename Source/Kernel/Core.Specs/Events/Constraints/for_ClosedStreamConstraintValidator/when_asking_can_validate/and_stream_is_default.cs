// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ClosedStreamConstraintValidator.when_asking_can_validate;

public class and_stream_is_default : given.a_closed_stream_constraint_validator
{
    bool _result;

    ConstraintValidationContext _context;

    void Establish() =>
        _context = new(
            [],
            EventSourceId.New(),
            new EventTypeId("SomeEvent"),
            new ExpandoObject(),
            eventStreamType: EventStreamType.All,
            eventStreamId: EventStreamId.Default);

    void Because() => _result = _validator.CanValidate(_context);

    [Fact] void should_not_be_able_to_validate() => _result.ShouldBeFalse();
}
