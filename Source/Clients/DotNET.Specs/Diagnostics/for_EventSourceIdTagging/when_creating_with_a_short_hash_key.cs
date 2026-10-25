// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.for_EventSourceIdTagging;

public class when_creating_with_a_short_hash_key : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => new EventSourceIdTagging(new ChronicleTelemetryOptions
    {
        EventSourceIdTracing = EventSourceIdTracing.Hashed,
        EventSourceIdHashKey = new string('k', IdentifierHasher.MinimumKeyLength - 1)
    }));

    [It] void should_fail_before_an_id_can_be_tagged() => _exception.ShouldBeOfExactType<InvalidIdentifierHashKey>();
    [It] void should_not_include_the_key_in_the_error() => _exception.Message.ShouldNotContain(new string('k', IdentifierHasher.MinimumKeyLength - 1));
}
