// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosedStreamConstraintValidator.when_validating;

public class and_no_dimensions_are_in_use : given.a_closed_stream_constraint_validator
{
    ConstraintValidationResult _result;

    void Establish() => _validator = new(_storage, []);

    async Task Because() => _result = await _validator.Validate(new([], EventSourceId.New(), "SomeEvent", new ExpandoObject(), EventSourceType.Default, EventStreamType.All, EventStreamId.Default));

    [Fact] void should_accept_the_append() => _result.IsValid.ShouldBeTrue();
    [Fact] async Task should_not_call_storage() => await _storage.DidNotReceive().GetCovering(Arg.Any<ClosedStreamScope>(), Arg.Any<IEnumerable<ClosedStreamDimensions>>());
}
