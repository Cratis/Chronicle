// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_federation_is_disabled : given.a_link_pump
{
    TimeSpan? _delay;

    void Establish()
    {
        _options = Options.Create(new ChronicleOptions { Federation = new Cratis.Chronicle.Configuration.Federation { Enabled = false } });
        _pump = CreatePump(BindingId, _pull, _clients, _inbox);
    }

    async Task Because() => _delay = await _pump.Step(default);

    [Fact] void should_stop() => _delay.ShouldBeNull();
    [Fact] void should_be_disabled() => _status.GetBinding(BindingId).State.ShouldEqual(FederationBindingStates.Disabled);
    [Fact] void should_pull_nothing() => _pull.Requests.ShouldBeEmpty();
}
