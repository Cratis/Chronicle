// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_a_component_is_null : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    Option<UniqueConstraintValue> _allNull;
    Option<UniqueConstraintValue> _oneNull;

    void Because()
    {
        _allNull = UniqueConstraintEncoder.Encode(_key, [new(null), new(null)]);
        _oneNull = UniqueConstraintEncoder.Encode(_key, [new("x"), new(null)]);
    }

    [Fact] void should_not_claim_when_every_component_is_null() => _allNull.TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_claim_when_one_component_is_present() => _oneNull.TryGetValue(out _).ShouldBeTrue();
}
