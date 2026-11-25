// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_OutboxEventTypeQueryFilter.when_narrowing;

public class the_outbox_with_another_casing : given.a_mixed_catalog
{
    string[] _upper;
    string[] _mixed;

    async Task Because()
    {
        await Narrow("OUTBOX");
        _upper = Ids(_result);
        await Narrow("Outbox");
        _mixed = Ids(_result);
    }

    [Fact] void should_restrict_uppercase_to_public_types() => _upper.ShouldContainOnly("public-one", "public-two");
    [Fact] void should_restrict_mixed_case_to_public_types() => _mixed.ShouldContainOnly("public-one", "public-two");
}
