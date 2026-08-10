// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

/// <summary>
/// The original Orleans grain method is a binary contract: changing only its return type changes the metadata
/// signature existing callers bind to. Result-aware coordination therefore has a distinct additive method.
/// </summary>
public class when_inspecting_the_public_contract : Specification
{
    Type _interfaceSeedReturnType;
    Type _implementationSeedReturnType;
    Type _resultAwareReturnType;

    void Because()
    {
        var parameters = new[] { typeof(IEnumerable<SeedingEntry>) };
        _interfaceSeedReturnType = typeof(IEventSeeding).GetMethod(nameof(IEventSeeding.Seed), parameters)!.ReturnType;
        _implementationSeedReturnType = typeof(EventSeeding).GetMethod(nameof(EventSeeding.Seed), parameters)!.ReturnType;
        _resultAwareReturnType = typeof(IResultAwareEventSeeding).GetMethod(nameof(IResultAwareEventSeeding.SeedWithResult), parameters)!.ReturnType;
    }

    [Fact] void should_keep_the_previous_interface_return_type() => _interfaceSeedReturnType.ShouldEqual(typeof(Task));
    [Fact] void should_keep_the_previous_implementation_return_type() => _implementationSeedReturnType.ShouldEqual(typeof(Task));
    [Fact] void should_add_a_result_aware_operation() => _resultAwareReturnType.ShouldEqual(typeof(Task<SeedingResult>));
}
