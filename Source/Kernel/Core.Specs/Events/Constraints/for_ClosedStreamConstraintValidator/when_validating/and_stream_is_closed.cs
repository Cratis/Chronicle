// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosedStreamConstraintValidator.when_validating;

public class and_stream_is_closed : given.a_closed_stream_constraint_validator
{
    ConstraintValidationResult _result;

    EventStreamType _streamType = new("invoices");
    EventStreamId _streamId = new("invoices-001");
    ConstraintValidationContext _context;

    void Establish()
    {
        _context = new(
            [],
            EventSourceId.New(),
            new EventTypeId("SomeEvent"),
            new ExpandoObject(),
            eventStreamType: _streamType,
            eventStreamId: _streamId);

        _storage.IsStreamClosed(_streamType, _streamId).Returns(true);
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_violations() => _result.Violations.ShouldNotBeEmpty();
    [Fact] void should_have_stream_closed_constraint_type() => _result.Violations[0].ConstraintType.ShouldEqual(ConstraintType.StreamClosed);
}
