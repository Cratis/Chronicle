// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

public class and_the_stream_closes : given.a_client_subscription_keeper
{
    Task Because() => KeepUntilTheStreamCloses();

    [Fact] void should_complete() => _completed.ShouldBeTrue();
    [Fact] void should_stop_checking() => _checks.ShouldEqual(ChecksBeforeTheStreamCloses);
}
