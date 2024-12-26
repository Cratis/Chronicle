// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_UniqueConstraintValidator.given;

public class a_unique_constraint_validator_with_valid_definition : a_unique_constraint_validator
{
    protected const string Property = "SomeProperty";

    protected ConstraintValidationContext _context;

    protected EventType _eventType = new("SomeEvent", 1);

    protected ExpandoObject _content;

    void Establish()
    {
        _content = new();
        _context = new([], EventSourceId.New(), _eventType.Id, _content);
    }

    protected override UniqueConstraintDefinition Definition => new("SomeConstraint", [new(_eventType.Id, [Property])]);

    protected void SetPropertyValue(object value) => (_content as IDictionary<string, object>)[Property] = value;
}
