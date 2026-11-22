// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2;

public class when_building_v2_identities
{
    [Fact]
    public void should_frame_components_instead_of_concatenating_separators() =>
        V2Identity.Digest("a+b", "c").ShouldNotEqual(V2Identity.Digest("a", "b+c"));

    [Fact]
    public void should_keep_exact_unicode_and_trailing_spaces()
    {
        V2Identity.Digest("é").ShouldNotEqual(V2Identity.Digest("e\u0301"));
        V2Identity.Digest("owner").ShouldNotEqual(V2Identity.Digest("owner "));
        V2Identity.Digest("Owner").ShouldNotEqual(V2Identity.Digest("owner"));
    }

    [Fact]
    public void should_reject_unpaired_surrogates_instead_of_replacing_them() =>
        Assert.Throws<InvalidUniqueConstraintV2Input>(() => V2Identity.Digest("\ud800"));

    [Fact]
    public void should_build_bounded_v1_disjoint_names_for_each_mode()
    {
        var source = new V2Binding(new string('s', 1000), "constraint+constraint", UniqueConstraintMode.PerEventSource);
        var value = source with { Mode = UniqueConstraintMode.PerValue };
        source.Physical.Length.ShouldBeLessThan(64);
        source.Physical.ShouldNotEqual(value.Physical);
        source.Physical.EndsWith("+uc2", StringComparison.Ordinal).ShouldBeTrue();
        value.Physical.EndsWith("+uc2v", StringComparison.Ordinal).ShouldBeTrue();
        source.Document["Sequence"].AsString.ShouldEqual(source.Sequence);
    }
}
