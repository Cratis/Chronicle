// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_upcasting_protected_content;

public class and_the_event_is_appended_in_a_batch : and_the_migration_splits_combines_and_maps_pii
{
    protected override bool ThroughBatch => true;

    [Fact] void should_split_plaintext_in_the_batch() => _released["splitFirst"]!.GetValue<string>().ShouldEqual("Jane");
    [Fact] void should_combine_plaintext_in_the_batch() => _released["combined"]!.GetValue<string>().ShouldEqual("Jane Austen");
    [Fact] void should_map_plaintext_in_the_batch() => _released["mapped"]!.GetValue<string>().ShouldEqual("new");
    [Fact] void should_protect_target_defaults_in_the_batch() => _protectedTarget["defaulted"]!.GetValue<string>().ShouldNotEqual("personal default");
    [Fact] void should_erase_all_migrated_batch_properties() => _erased.All(property => property.Value!.GetValue<string>().Length == 0).ShouldBeTrue();
}
