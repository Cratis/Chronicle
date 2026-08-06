// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_deserializing_a_unique_event_type_constraint;

/// <summary>
/// The removal event has to survive the store. A definition that loses it on the way back in reverts to "at most one,
/// forever" the first time the kernel reads its constraints from the collection rather than from a fresh
/// registration — so the constraint would behave as declared until a restart and silently stop releasing after one.
/// </summary>
public class and_it_declares_a_removal_event : given.a_stored_constraint_definition
{
    static readonly EventTypeId _coveredEventTypeId = "the-event-type";
    static readonly EventTypeId _removalEventTypeId = "the-removal-event-type";
    static readonly UniqueEventTypeConstraintDefinition _definition = new(ConstraintNameValue, [_coveredEventTypeId], _removalEventTypeId);

    IConstraintDefinition _result;

    void Because() => _result = Read(Write(_definition));

    [Fact] void should_read_back_the_definition_that_was_written() => _result.ShouldEqual(_definition);
    [Fact] void should_keep_the_removal_event() => ((UniqueEventTypeConstraintDefinition)_result).RemovedWith.ShouldEqual(_removalEventTypeId);
}
