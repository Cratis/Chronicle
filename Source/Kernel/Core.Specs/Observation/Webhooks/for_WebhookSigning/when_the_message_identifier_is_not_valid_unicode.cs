// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning;

public class when_the_message_identifier_is_not_valid_unicode : given.a_signing_fixture
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => WebhookSigning.Sign("msg_\ud800", 1700000000, _payload, _secret));

    [Fact] void should_not_silently_replace_an_unpaired_surrogate() => _error.ShouldBeOfExactType<EncoderFallbackException>();
}
