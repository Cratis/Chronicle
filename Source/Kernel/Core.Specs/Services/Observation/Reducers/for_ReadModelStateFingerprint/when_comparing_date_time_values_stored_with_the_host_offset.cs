// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_ReadModelStateFingerprint;

/// <summary>
/// A <c language="json">date-time</c> value stored with the host's offset used to be held as a local <see cref="DateTime"/> and is now
/// held as a <see cref="DateTimeOffset"/> with that offset. Its fingerprint must not change.
/// </summary>
public class when_comparing_date_time_values_stored_with_the_host_offset : given.states
{
    static readonly DateTime _local = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Local).AddTicks(1234567);
    static readonly DateTimeOffset _withHostOffset = new(_local);

    [Fact] void should_fingerprint_the_value_as_it_did_before() => Of(_withHostOffset).ShouldEqual(Of(_local));
    [Fact] void should_fingerprint_state_holding_the_value_as_it_did_before() =>
        Of(Expando(("moment", _withHostOffset))).ShouldEqual(Of(Expando(("moment", _local))));
}
