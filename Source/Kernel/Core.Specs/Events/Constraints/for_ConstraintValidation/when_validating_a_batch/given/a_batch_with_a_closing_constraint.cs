// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.when_validating_a_batch.given;

public class a_batch_with_a_closing_constraint : Specification
{
    protected ClosedStreamsConstraintStorage _storage;
    protected ConstraintValidation _validation;
    protected ConstraintBatchClaims _claims;
    protected readonly ClosedStreamScope _scope = new(EventSourceId: "source", EventStreamType: "transactions", EventStreamId: "month");

    void Establish()
    {
        _storage = new();
        _claims = new();
        var definition = new ClosesStreamConstraintDefinition("closing", ["Closed"], _scope.Dimensions, ["Reopened"]);
        _validation = new([new ClosesStreamConstraintValidator(definition, _storage), new ClosedStreamConstraintValidator(_storage, [definition.Dimensions], [definition])]);
    }

    protected ConstraintValidationContext ContextFor(EventTypeId eventType) =>
        _validation.Establish("source", eventType, new ExpandoObject(), EventSourceType.Default, "transactions", "month", _claims);
}
