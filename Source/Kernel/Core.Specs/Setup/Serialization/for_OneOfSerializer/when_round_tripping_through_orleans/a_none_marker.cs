// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf.Types;

namespace Cratis.Chronicle.Setup.Serialization.for_OneOfSerializer.when_round_tripping_through_orleans;

public class a_none_marker : given.a_configured_orleans_serializer
{
    bool _succeeded;

    void Because()
    {
        RoundTrip(default(None));
        _succeeded = true;
    }

    [Fact] void should_round_trip_without_a_missing_codec() => _succeeded.ShouldBeTrue();
}
