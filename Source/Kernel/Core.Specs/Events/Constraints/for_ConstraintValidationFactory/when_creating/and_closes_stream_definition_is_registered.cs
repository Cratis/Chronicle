// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationFactory.when_creating;

public class and_closes_stream_definition_is_registered : given.a_constraint_validation_factory
{
    IConstraintValidation _result;

    void Establish() => _definitions.Add(new ClosesStreamConstraintDefinition("closing", ["Closed"], ClosedStreamDimensions.EventSourceId, []));

    async Task Because() => _result = await _factory.Create(KeyFor(EventSequenceId.Log));

    [Fact] void should_create_the_closing_validator() => _result.Establish("source", "Closed", new ExpandoObject(), EventSourceType.Default, EventStreamType.All, EventStreamId.Default).Validators.OfType<ClosesStreamConstraintValidator>().Count().ShouldEqual(1);
}
