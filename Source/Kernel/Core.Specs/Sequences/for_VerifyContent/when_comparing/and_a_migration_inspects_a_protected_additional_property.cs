// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migration_inspects_a_protected_additional_property : given.a_migrated_event_with_dynamic_and_nested_protected_values
{
    async Task Establish() => await Store("""{"detail":{"$split":{"source":"contacts.home","separator":"=","part":0}}}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_reject_a_migration_that_inspects_protected_plaintext() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
