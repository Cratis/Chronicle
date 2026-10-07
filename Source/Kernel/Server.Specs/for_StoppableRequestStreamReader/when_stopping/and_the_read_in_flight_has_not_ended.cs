// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_StoppableRequestStreamReader.when_stopping;

public class and_the_read_in_flight_has_not_ended : given.a_reader_with_a_read_in_flight
{
    Task _stopping;

    protected override bool ReadEndsWhenCancelled => false;

    void Because() => _stopping = _reader.Stop();

    [Fact] void should_not_have_stopped_yet() => _stopping.IsCompleted.ShouldBeFalse();

    void Destroy() => _pendingRead.TrySetResult(false);
}
