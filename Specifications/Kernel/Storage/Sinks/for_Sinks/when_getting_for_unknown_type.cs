// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Storage.Sinks.for_Sinks;

public class when_getting_for_unknown_type : Specification
{
    Sinks sinks;
    Exception result;

    void Establish() => sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>(Array.Empty<ISinkFactory>()));

    void Because() => result = Catch.Exception(() => sinks.GetFor("bc5e82fd-9845-4464-9802-a7e21bd8a919", new Model(string.Empty, null)));

    [Fact] void should_throw_unknown_result_store() => result.ShouldBeOfExactType<UnknownSink>();
}
