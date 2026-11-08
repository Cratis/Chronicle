// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.Reducers.for_RememberedStateSizes;

/// <summary>
/// Reading a size counts as a use, so a partition that keeps being reduced is not forgotten when the capacity is reached.
/// </summary>
public class when_a_partition_keeps_being_reduced_while_others_come_and_go : Specification
{
    const string Scope = "store/namespace/read-model";
    const int Capacity = 10;
    const long KeptSize = 42;

    RememberedStateSizes _sizes;
    Key _kept;
    bool _keptRemembered;
    long _keptSize;

    void Establish()
    {
        _sizes = new(Capacity);
        _kept = new Key("kept", ArrayIndexers.NoIndexers);
        _sizes.Remember(Scope, _kept, KeptSize);
    }

    void Because()
    {
        for (var index = 0; index < Capacity * 10; index++)
        {
            _sizes.Remember(Scope, new Key(index.ToString(), ArrayIndexers.NoIndexers), index);
            _sizes.TryGet(Scope, _kept, out _);
        }

        _keptRemembered = _sizes.TryGet(Scope, _kept, out _keptSize);
    }

    [Fact] void should_still_remember_the_partition() => _keptRemembered.ShouldBeTrue();
    [Fact] void should_still_remember_its_size() => _keptSize.ShouldEqual(KeptSize);
    [Fact] void should_hold_no_more_than_its_capacity() => _sizes.Count.ShouldBeLessThanOrEqual(Capacity);
}
