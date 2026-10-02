// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.when_getting_for_name;

/// <summary>
/// Two providers can declare the same name. Resolving the message of a genuine violation looks the definition up by
/// name, and used to throw "Sequence contains more than one matching element" instead of reporting the violation.
/// </summary>
public class and_more_than_one_definition_has_the_name : given.no_constraints
{
    static readonly ConstraintName _sharedName = "SharedName";

    IConstraintDefinition _first;
    IConstraintDefinition _second;
    IConstraintDefinition _result;
    ConstraintViolation _resolved;

    async Task Establish()
    {
        _first = new UniqueEventTypeConstraintDefinition(_sharedName, _ => (ConstraintViolationMessage)"First", [new EventTypeId("FirstEvent")], []);
        _second = new UniqueEventTypeConstraintDefinition(_sharedName, _ => (ConstraintViolationMessage)"Second", [new EventTypeId("SecondEvent")], []);
        _constraintsProvider.Provide().Returns(new[] { _first, _second }.ToImmutableList());
        await _constraints.Discover();
    }

    void Because()
    {
        _result = _constraints.GetFor(_sharedName);
        _resolved = _constraints.ResolveMessageFor(new ConstraintViolation(
            "FirstEvent",
            EventSequenceNumber.First,
            ConstraintType.UniqueEventType,
            _sharedName,
            string.Empty,
            new()));
    }

    [Fact] void should_return_the_first_definition() => _result.ShouldEqual(_first);
    [Fact] void should_resolve_the_message_of_the_violation() => _resolved.Message.Value.ShouldEqual("First");
}
