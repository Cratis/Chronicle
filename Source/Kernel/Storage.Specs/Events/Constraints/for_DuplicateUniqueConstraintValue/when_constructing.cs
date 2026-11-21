// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints.for_DuplicateUniqueConstraintValue;

public class when_constructing : Specification
{
    const string Constraint = "unique-name";
    const string Claimant = "claimant@example.com";
    DuplicateUniqueConstraintValue _exception;

    void Because() => _exception = new(Constraint, Claimant);

    [Fact] void should_identify_the_constraint_in_the_message() => _exception.Message.ShouldContain(Constraint);
    [Fact] void should_not_include_the_event_source_id_in_the_message() => _exception.Message.ShouldNotContain(Claimant);
    [Fact] void should_not_include_the_event_source_id_in_the_exception_text() => _exception.ToString().ShouldNotContain(Claimant);
    [Fact] void should_keep_the_constraint_name_available_programmatically() => _exception.ConstraintName.ShouldEqual((ConstraintName)Constraint);
    [Fact] void should_keep_the_event_source_id_available_programmatically() => _exception.EventSourceId.ShouldEqual((EventSourceId)Claimant);
}
