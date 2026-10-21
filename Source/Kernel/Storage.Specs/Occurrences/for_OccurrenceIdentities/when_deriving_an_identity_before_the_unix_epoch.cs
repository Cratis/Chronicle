// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences.for_OccurrenceIdentities;

public class when_deriving_an_identity_before_the_unix_epoch : Specification
{
    OccurrenceId _identity;

    void Because() => _identity = OccurrenceIdentities.ForSchedule("token", -1);

    [Fact] void should_encode_the_signed_epoch_second_in_the_publication_format() => _identity.Value.ShouldEqual("C70F566C3EBAB2755EF6DDE401E41125C8BB257427712A0CFE6277A5CB49A28A");
}
