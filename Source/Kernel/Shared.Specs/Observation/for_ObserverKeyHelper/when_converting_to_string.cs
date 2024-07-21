// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_ObserverKeyHelper;

public class when_converting_to_string : Specification
{
    ObserverKey input;
    string result;

    void Establish() => input = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

    void Because() => result = input.ToString();

    [Fact] void should_combine_correctly() => result.ShouldEqual($"{input.EventStore}+{input.Namespace}+{input.EventSequenceId}");
}
