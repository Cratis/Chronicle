// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ContractMode = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookSigningMode;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_an_unsupported_signing_batch : given.a_pinned_registration
{
    [Theory]
    [InlineData(3)]
    [InlineData(99)]
    async Task should_reject_the_whole_batch_before_appending(int unsupported)
    {
        var invalid = new Contracts.Observation.Webhooks.WebhookDefinition { Identifier = "invalid", Signing = (ContractMode)unsupported };
        var error = await Catch.Exception(() => _registrar.Add("store", [_definition, invalid]));
        error.ShouldBeOfExactType<WebhookSigningNotSupported>();
        _appended.ShouldBeEmpty();
        await _manager.DidNotReceive().GetWebhookDefinitions();
    }
}
