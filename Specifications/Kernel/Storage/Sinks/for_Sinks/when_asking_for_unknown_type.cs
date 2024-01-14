// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Storage.Sinks.for_Sinks;

public class when_asking_for_unknown_type : Specification
{
    Sinks sinks;
    bool result;

    void Establish() => sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>(Array.Empty<ISinkFactory>()));

    void Because() => result = sinks.HasType("bc5e82fd-9845-4464-9802-a7e21bd8a919");

    [Fact] void should_not_have_type() => result.ShouldBeFalse();
}
