// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberSelector;

public class when_creating_with_unknown_strategy : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _ = new ObserverSubscriberSelector(
        Options.Create(new ChronicleOptions { Observers = new() { FanOutStrategy = "unknown" } })));

    [Fact] void should_throw_unknown_fan_out_strategy() => _exception.ShouldBeOfExactType<UnknownFanOutStrategy>();
}
