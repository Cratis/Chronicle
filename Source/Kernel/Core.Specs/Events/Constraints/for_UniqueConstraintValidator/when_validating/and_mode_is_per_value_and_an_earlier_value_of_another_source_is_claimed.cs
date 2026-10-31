// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_validating;

public class and_mode_is_per_value_and_an_earlier_value_of_another_source_is_claimed : Specification
{
    UniqueConstraintValidator _validator;
    ConstraintValidationContext _context;
    ConstraintValidationResult _result;

    async Task Establish()
    {
        var definition = new UniqueConstraintDefinition("versions", [new("added", ["Id"])]) { Mode = UniqueConstraintMode.PerValue };
        var storage = new Storage.InMemory.Events.Constraints.UniqueConstraintsStorage();
        var owner = EventSourceId.New();
        await storage.Save(owner, definition, 42L, "4f7aa54b8d9a8f5e7b06bf38217a84dfd7272bd50f5aebe97ae321f24eceb291");
        await storage.Save(owner, definition, 43L, "another-hash");
        _validator = new(definition, storage);
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["Id"] = "SomeValue";
        _context = new([], EventSourceId.New(), "added", content);
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_refuse_the_earlier_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_identify_the_original_claim() => _result.Violations.Single().SequenceNumber.ShouldEqual((EventSequenceNumber)42L);
}
