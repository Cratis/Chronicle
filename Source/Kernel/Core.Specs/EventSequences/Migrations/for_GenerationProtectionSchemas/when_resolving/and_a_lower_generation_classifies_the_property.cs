// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_a_lower_generation_classifies_the_property : for_ProtectedEventTypeMigrations.given.a_token_protected_only_in_the_latest_generation
{
    JsonSchema _effective;

    void Because() => _effective = GenerationProtectionSchemas.For(2, [((EventTypeGeneration)1, _schemas[2]), ((EventTypeGeneration)2, _schemas[0])]);

    [Fact] void should_leave_the_highest_generation_unchanged() => _effective.ShouldBeSame(_schemas[0]);
}
