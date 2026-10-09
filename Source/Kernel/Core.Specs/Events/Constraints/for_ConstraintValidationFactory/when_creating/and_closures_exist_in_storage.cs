// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationFactory.when_creating;

public class and_closures_exist_in_storage : given.a_constraint_validation_factory
{
    ConstraintValidationResult _result;

    async Task Establish()
    {
        var storage = new ClosedStreamsConstraintStorage();
        await storage.Close(new(new(EventSourceId: "source-a"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
        _namespaceStorage.GetClosedStreamsConstraints(EventSequenceId.Log).Returns(storage);
    }

    async Task Because()
    {
        var validation = await _factory.Create(KeyFor(EventSequenceId.Log));
        _result = await validation.Establish("source-a", "SomeEvent", new ExpandoObject(), EventSourceType.Default, EventStreamType.All, EventStreamId.Default).Validate();
    }

    [Fact] void should_bootstrap_masks_from_storage() => _result.IsValid.ShouldBeFalse();
}
