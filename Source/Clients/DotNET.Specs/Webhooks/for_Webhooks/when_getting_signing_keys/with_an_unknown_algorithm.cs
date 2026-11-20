// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using ContractKeyStatus = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookSigningKeyStatus;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_getting_signing_keys;

public class with_an_unknown_algorithm : given.signing_key_query_responses
{
    string[] _keyIds;

    void Establish()
    {
        var hmac = Key("hmac", ContractKeyStatus.Active);
        hmac.Algorithm = WebhookSigningAlgorithm.HmacSha256;
        _result = Contracts.Queries.QueryResult<IEnumerable<WebhookSigningPublicKeyResponse>>.Success(
            Guid.Empty,
            [hmac, Key("retired", ContractKeyStatus.Retired), Key("ed", ContractKeyStatus.Active)]);
    }

    async Task Because() => _keyIds = (await _webhooks.GetSigningKeys()).Select(_ => _.KeyId).ToArray();

    [Fact] void should_only_return_the_active_ed25519_key() => _keyIds.ShouldEqual(["ed"]);
}
