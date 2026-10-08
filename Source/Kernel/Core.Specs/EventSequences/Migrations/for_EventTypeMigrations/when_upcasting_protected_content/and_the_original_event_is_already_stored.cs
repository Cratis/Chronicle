// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_upcasting_protected_content;

public class and_the_original_event_is_already_stored : and_the_migration_splits_combines_and_maps_pii
{
    protected override bool FromStoredContent => true;

    [Fact] void should_release_under_the_original_subject_before_splitting() => _released["splitFirst"]!.GetValue<string>().ShouldEqual("Jane");
    [Fact] void should_release_under_the_original_subject_before_combining() => _released["combined"]!.GetValue<string>().ShouldEqual("Jane Austen");
    [Fact] void should_release_under_the_original_subject_before_mapping() => _released["mapped"]!.GetValue<string>().ShouldEqual("new");
    [Fact] void should_protect_new_generation_defaults() => _protectedTarget["defaulted"]!.GetValue<string>().ShouldNotEqual("personal default");
    [Fact] void should_erase_the_new_generation() => _erased.All(property => property.Value!.GetValue<string>().Length == 0).ShouldBeTrue();
}
