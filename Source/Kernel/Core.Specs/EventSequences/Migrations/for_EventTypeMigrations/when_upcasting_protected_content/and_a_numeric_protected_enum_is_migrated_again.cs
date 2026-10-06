// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_upcasting_protected_content;

public class and_a_numeric_protected_enum_is_migrated_again : and_a_protected_enum_is_migrated_twice
{
    protected override bool UsesNumericCiphertext => true;

    [Fact] void should_complete_the_numeric_ciphertext_migration() => _error.ShouldBeNull();
    [Fact] void should_map_the_numeric_enum_to_the_new_value() => ((IDictionary<string, object?>)_converter.ToExpandoObject(_released, _thirdSchema))["status"].ShouldEqual(7);
    [Fact] void should_protect_the_target_of_the_numeric_enum() => _protectedTarget["status"]!.GetValue<string>().ShouldNotEqual("Accepted");
}
