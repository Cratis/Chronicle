// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.given;

public class a_lagging_legacy_registration : a_pinned_registration
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(ALifecycleEvent<WebhookAdded>(5));
        TheStoredRegistrationHasFormat(WebhookDeliveryFormat.Legacy);
        _definition.Target.Authorization = new(new Contracts.Security.BearerTokenAuthorization { Token = "first" });
        _encryption.Encrypt(Arg.Any<string>()).Returns(call => $"encrypted-{call.Arg<string>()}");
    }
}
