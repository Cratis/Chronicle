// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ImmediateProjectionKey.when_round_tripping;

public class with_a_session_and_a_separator : Specification
{
    ImmediateProjectionKey _key;
    ImmediateProjectionKey _result;

    void Establish() => _key = new("projection", "store", "namespace", "sequence", "model#part#key#", Guid.Parse("a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6"));

    void Because() => _result = ImmediateProjectionKey.Parse(_key.ToString());

    [Fact] void should_preserve_all_components() => _result.ShouldEqual(_key);
}
