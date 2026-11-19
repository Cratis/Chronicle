// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningPublicKey.given;

public class a_public_key_query : Signing.for_WebhookDeliverySigner.given.a_delivery_signer
{
    protected WebhookSigningPublicKey[] _result;
    protected string _serialized;

    protected async Task Query()
    {
        _result = (await WebhookSigningPublicKey.GetWebhookSigningKeys("store", _grainFactory)).ToArray();
        _serialized = JsonSerializer.Serialize(_result);
    }
}
