// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_no_higher_generation_adds_metadata : for_ProtectedEventTypeMigrations.given.a_token_protected_only_in_the_latest_generation
{
    JsonSchema _effective;

    void Because() => _effective = GenerationProtectionSchemas.For(1, [((EventTypeGeneration)1, _schemas[0]), ((EventTypeGeneration)2, _schemas[1])]);

    [Fact] void should_return_the_registered_instance() => _effective.ShouldBeSame(_schemas[0]);
}
