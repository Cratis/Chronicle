// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_Constraints;

public class when_resolving_message_for_violation : given.two_constraints
{
    const string _firstProperty = "firstProperty";
    const string _secondProperty = "secondProperty";
    ConstraintViolation _violation;
    string _firstPropertyValue;
    string _secondPropertyValue;
    ConstraintViolation _result;

    void Establish()
    {
        _firstPropertyValue = Guid.NewGuid().ToString();
        _secondPropertyValue = Guid.NewGuid().ToString();

        _violation = new ConstraintViolation(
            "SomeEvent",
            EventSequenceNumber.First,
            _firstConstraintName,
            string.Empty,
            new()
            {
                { _firstProperty, _firstPropertyValue },
                { _secondProperty, _secondPropertyValue }
            });
    }

    protected override ConstraintViolationMessage FirstConstraintMessageProvider(ConstraintViolation violation) => $"First {{{_firstProperty}}} second {{{_secondProperty}}}";

    void Because() => _result = _constraints.ResolveMessageFor(_violation);

    [Fact] void should_resolve_to_expected_string() => _result.Message.Value.ShouldEqual($"First {_firstPropertyValue} second {_secondPropertyValue}");
}
