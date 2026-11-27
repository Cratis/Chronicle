// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;
using Wire = Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_serializing_for_the_wire;

public class and_the_distance_is_zero_or_absent : Specification
{
    Wire.ObserverLagSample _measuredZero;
    Wire.ObserverLagSample _absent;

    void Because()
    {
        _measuredZero = RoundTrip(new Wire.ObserverLagSample { Status = Wire.ObserverLagStatus.CaughtUp, Distance = 0UL, NextEventSequenceNumber = 0UL });
        _absent = RoundTrip(new Wire.ObserverLagSample { Status = Wire.ObserverLagStatus.Unmeasurable });
    }

    [Fact] void should_keep_a_measured_zero_distance() => _measuredZero.Distance.ShouldEqual(0UL);
    [Fact] void should_keep_a_measured_zero_position() => _measuredZero.NextEventSequenceNumber.ShouldEqual(0UL);
    [Fact] void should_keep_an_absent_distance_absent() => _absent.Distance.ShouldBeNull();
    [Fact] void should_keep_an_absent_position_absent() => _absent.NextEventSequenceNumber.ShouldBeNull();

    static Wire.ObserverLagSample RoundTrip(Wire.ObserverLagSample sample)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, sample);
        stream.Position = 0;
        return Serializer.Deserialize<Wire.ObserverLagSample>(stream);
    }
}
