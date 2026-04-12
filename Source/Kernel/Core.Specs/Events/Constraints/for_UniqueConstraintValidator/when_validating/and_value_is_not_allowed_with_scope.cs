// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_validating;

public class and_value_is_not_allowed_with_scope : Specification
{
    UniqueConstraintValidator _validator;
    IUniqueConstraintsStorage _storage;
    ConstraintValidationContext _context;
    ConstraintValidationResult _result;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        var definition = new UniqueConstraintDefinition(
            "ScopedConstraint",
            [new("SomeEvent", ["SomeProperty"])],
            Scope: new ConstraintScope(EventSourceType: "MySourceType", EventStreamType: "MyStreamType"));

        _validator = new UniqueConstraintValidator(definition, _storage);

        var contentAsExpando = new ExpandoObject();
        dynamic content = contentAsExpando;
        content.SomeProperty = "SomeValue";

        _context = new([], EventSourceId.New(), "SomeEvent", contentAsExpando, eventSourceType: "MySourceType", eventStreamType: "MyStreamType");

        _storage.IsAllowed(
            Arg.Any<EventSourceId>(),
            Arg.Any<UniqueConstraintDefinition>(),
            Arg.Any<UniqueConstraintValue>(),
            "est:MySourceType|estt:MyStreamType").Returns((false, 42U));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_violations() => _result.Violations.ShouldNotBeEmpty();
    [Fact] void should_pass_scope_key_to_storage() => _storage.Received(1).IsAllowed(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>(), "est:MySourceType|estt:MyStreamType");
}
