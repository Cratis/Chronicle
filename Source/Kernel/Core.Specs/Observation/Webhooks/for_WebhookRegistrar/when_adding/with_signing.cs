// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using ContractMode = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookSigningMode;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_signing : given.a_pinned_registration
{
    void Establish() => _definition.Signing = ContractMode.Ed25519;
    async Task Because() => await _registrar.Add("store", [_definition]);
    [Fact] void should_record_the_policy() => _appended.OfType<WebhookAdded>().Single().Signing.ShouldEqual(WebhookSigningMode.Ed25519);
}
