// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyLocations;

public class when_building_locations : Specification
{
    VaultKeyLocations _ordinary;
    VaultKeyLocations _unresolved;

    void Because()
    {
        _ordinary = VaultKeyLocations.For("store", "default", "customer-42");
        _unresolved = VaultKeyLocations.For("store", "default", "a/b");
    }

    [Fact] void should_place_the_directory_under_the_root() => _ordinary.Directory.ShouldEqual("k1/store/default/customer-42");
    [Fact] void should_encode_each_segment_of_the_directory() => _unresolved.Directory.ShouldEqual("k1/store/default/a~2Fb");
    [Fact] void should_use_the_raw_triple_as_the_earlier_directory_when_it_is_eligible() => _ordinary.LegacyDirectory.ShouldEqual("store/default/customer-42");
    [Fact] void should_not_have_an_unresolved_directory_when_it_is_eligible() => _ordinary.UnresolvedLegacyDirectory.ShouldBeNull();
    [Fact] void should_not_have_an_earlier_directory_when_it_is_not_eligible() => _unresolved.LegacyDirectory.ShouldBeNull();
    [Fact] void should_use_the_raw_triple_as_the_unresolved_directory_when_it_is_not_eligible() => _unresolved.UnresolvedLegacyDirectory.ShouldEqual("store/default/a/b");
    [Fact] void should_place_the_erasure_beside_the_revisions() => _ordinary.ErasurePath.ShouldEqual("k1/store/default/customer-42/erasure");
}
