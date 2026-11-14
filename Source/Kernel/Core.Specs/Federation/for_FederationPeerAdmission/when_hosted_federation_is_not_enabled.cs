// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_hosted_federation_is_not_enabled : given.a_peer_admission
{
    async Task Because()
    {
        var certificate = await Issued("acme", "prod");
        await SetPolicy("acme");
        var disabled = new ChronicleOptions { Federation = new() { Enabled = true, Hosted = new() { Enabled = false, TrustDomain = "example" } } };
        _admitted = await new FederationPeerAdmission(_registry, _keys, Options.Create(disabled), _time).Admit(certificate);
    }

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_it_is_not_enabled() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.NotEnabled);
}
