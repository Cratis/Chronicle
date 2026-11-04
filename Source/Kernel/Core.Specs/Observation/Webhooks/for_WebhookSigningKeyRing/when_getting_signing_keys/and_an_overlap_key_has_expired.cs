// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_getting_signing_keys;

public class and_an_overlap_key_has_expired : given.a_signing_key_ring
{
    IEnumerable<WebhookSigningKey> _result;

    async Task Establish() => await CreateRing(Key("active", WebhookSigningKeyStatus.Active), Key("old", WebhookSigningKeyStatus.Overlap, Now.AddSeconds(-1)));

    async Task Because() => _result = await _ring.GetSigningKeys();

    [Fact] void should_append_a_retired_event() => _appended.Select(_ => _.Event).ShouldContainOnly([new WebhookSigningKeyRetired("old")]);
    [Fact] void should_not_return_the_expired_key() => _result.Select(_ => _.KeyId.Value).ShouldContainOnly(["active"]);
    [Fact] void should_persist_the_key_as_retired() => State.Keys.Single(_ => _.KeyId.Value == "old").Status.ShouldEqual(WebhookSigningKeyStatus.Retired);
    [Fact] void should_record_when_it_was_retired() => State.Keys.Single(_ => _.KeyId.Value == "old").RetiredAt.ShouldEqual(Now);
    [Fact] void should_erase_the_secret() => State.Keys.Single(_ => _.KeyId.Value == "old").EncryptedSecret.ShouldEqual(EncryptedWebhookSigningSecret.Empty);
}
