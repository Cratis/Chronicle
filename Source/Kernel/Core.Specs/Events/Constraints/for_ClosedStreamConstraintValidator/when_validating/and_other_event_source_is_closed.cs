// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosedStreamConstraintValidator.when_validating;

public class and_other_event_source_is_closed : Specification
{
    ClosedStreamConstraintValidator _validator;
    ConstraintValidationResult _result;

    async Task Establish()
    {
        var storage = new ClosedStreamsConstraintStorage();
        await storage.Close(new(new(EventSourceId: "source-b"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
        _validator = new(storage, [ClosedStreamDimensions.EventSourceId]);
    }

    async Task Because() => _result = await _validator.Validate(new([], "source-a", "SomeEvent", new ExpandoObject(), EventSourceType.Default, EventStreamType.All, EventStreamId.Default));

    [Fact] void should_accept_the_other_source() => _result.IsValid.ShouldBeTrue();
}
