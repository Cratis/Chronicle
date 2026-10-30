// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public class and_the_sink_has_not_opted_in : Specification
{
    ISink _sink;
    Exception _error;

    void Establish() => _sink = new given.a_legacy_sink();

    async Task Because() => _error = await Catch.Exception(() => _sink.ReplaceIsolated(new Key("one", ArrayIndexers.NoIndexers), new ExpandoObject(), 20));

    [Fact] void should_not_advertise_support() => _sink.SupportsIsolatedReplace.ShouldBeFalse();
    [Fact] void should_fail_explicitly() => _error.ShouldBeOfExactType<NotSupportedException>();
}
