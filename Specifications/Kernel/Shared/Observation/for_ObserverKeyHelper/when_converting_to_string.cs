// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverKeyHelper;

public class when_converting_to_string : Specification
{
    ObserverKey input;
    string result;

    void Establish() => input = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    void Because() => result = input.ToString();

    [Fact] void should_combine_correctly() => result.ShouldEqual($"{input.MicroserviceId}+{input.TenantId}+{input.EventSequenceId}");
}
