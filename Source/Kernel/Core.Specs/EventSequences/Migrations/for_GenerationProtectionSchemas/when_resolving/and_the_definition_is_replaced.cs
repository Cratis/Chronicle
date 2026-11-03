// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using EventTypesStorage = Cratis.Chronicle.Storage.InMemory.Events.EventTypes.EventTypesStorage;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_the_definition_is_replaced : for_ProtectedEventTypeMigrations.given.a_token_protected_only_in_the_latest_generation
{
    JsonSchema _original;
    JsonSchema _replacement;
    JsonSchema _originalAgain;

    async Task Because()
    {
        var storage = new EventTypesStorage();
        await storage.Register(_definition);
        _original = GenerationProtectionSchemas.For(await storage.GetDefinition(_definition.Id), 1);
        var replacement = _definition with
        {
            Generations =
            [
                new EventTypeGenerationDefinition(1, _schemas[0]),
                new EventTypeGenerationDefinition(2, _schemas[1]),
                new EventTypeGenerationDefinition(3, _schemas[0])
            ]
        };
        await storage.Register(replacement);
        _replacement = GenerationProtectionSchemas.For(await storage.GetDefinition(_definition.Id), 1);
        _originalAgain = GenerationProtectionSchemas.For(_definition, 1);
    }

    [Fact] void should_use_the_replacement_classification() => _replacement.HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_preserve_the_original_snapshot() => _originalAgain.ShouldBeSame(_original);
    [Fact] void should_keep_the_original_classification() => _original.HasSchemaMetadata().ShouldBeTrue();
}
