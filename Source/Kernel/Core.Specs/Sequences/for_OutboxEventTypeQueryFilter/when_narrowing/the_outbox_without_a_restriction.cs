// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_OutboxEventTypeQueryFilter.when_narrowing;

public class the_outbox_without_a_restriction : given.a_mixed_catalog
{
    async Task Because() => await Narrow("outbox");

    [Fact] void should_restrict_to_public_types() => Ids(_result).ShouldContainOnly("public-one", "public-two");
}
