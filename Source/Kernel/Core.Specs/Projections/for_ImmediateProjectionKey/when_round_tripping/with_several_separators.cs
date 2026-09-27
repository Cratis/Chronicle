// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ImmediateProjectionKey.when_round_tripping;

public class with_several_separators : Specification
{
    ImmediateProjectionKey _key;
    ImmediateProjectionKey _result;

    void Establish() => _key = new("projection", "store", "namespace", "sequence", "model#part#key");

    void Because() => _result = ImmediateProjectionKey.Parse(_key.ToString());

    [Fact] void should_preserve_all_components() => _result.ShouldEqual(_key);
}
