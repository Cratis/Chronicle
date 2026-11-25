// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Webhooks;
using ContractKeyStatus = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookSigningKeyStatus;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_getting_signing_keys;

public class with_no_overlap_end : given.signing_key_query_responses
{
    WebhookSigningPublicKey _key;

    void Establish() => _result = Contracts.Queries.QueryResult<IEnumerable<WebhookSigningPublicKeyResponse>>.Success(
        Guid.Empty,
        [Key("current", ContractKeyStatus.Active)]);

    async Task Because() => _key = (await _webhooks.GetSigningKeys()).Single();

    [Fact] void should_have_no_overlap_end() => _key.OverlapEndsAt.ShouldBeNull();
}
