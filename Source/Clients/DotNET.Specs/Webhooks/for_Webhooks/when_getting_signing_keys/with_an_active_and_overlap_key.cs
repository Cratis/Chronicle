// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Webhooks;
using ContractKeyStatus = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookSigningKeyStatus;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_getting_signing_keys;

public class with_an_active_and_overlap_key : given.signing_key_query_responses
{
    WebhookSigningPublicKey[] _keys;

    void Establish() => _result = Contracts.Queries.QueryResult<IEnumerable<WebhookSigningPublicKeyResponse>>.Success(
        Guid.Empty,
        [
            Key("new", ContractKeyStatus.Active),
            Key("old", ContractKeyStatus.Overlap, "2026-02-03T04:05:06.0000000+00:00")
        ]);

    async Task Because() => _keys = (await _webhooks.GetSigningKeys()).ToArray();

    [Fact] void should_return_both_keys() => _keys.Select(_ => _.KeyId).ShouldEqual(["new", "old"]);
    [Fact] void should_map_the_statuses() => _keys.Select(_ => _.Status).ShouldEqual([Cratis.Chronicle.Webhooks.WebhookSigningKeyStatus.Active, Cratis.Chronicle.Webhooks.WebhookSigningKeyStatus.Overlap]);
    [Fact] void should_map_the_public_key() => _keys[0].PublicKey.ShouldEqual("whpk_new");
    [Fact] void should_map_the_creation_time() => _keys[0].CreatedAt.ShouldEqual(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
    [Fact] void should_map_the_overlap_end() => _keys[1].OverlapEndsAt.ShouldEqual(new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero));
}
