// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.for_observer_information_wire;

public class when_the_timestamp_is_null : given.an_observer_information_payload
{
    byte[] _payload;
    byte[] _oldPayload;

    void Because()
    {
        _payload = Serialize(CreateInformation());
        _oldPayload = Serialize(CreateOldInformation());
    }

    [Fact] void should_emit_only_the_old_fields() => _payload.ShouldEqual(_oldPayload);
}
