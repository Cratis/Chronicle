// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A silo that is still leaving the cluster can fail a check or two; the subscription must still be kept after that.
/// </summary>
public class and_a_check_fails : given.a_client_subscription_keeper
{
    protected override Task Check(int check) =>
        check == 1 ? Task.FromException(new InvalidOperationException("The silo is leaving")) : Task.CompletedTask;

    Task Because() => KeepUntilTheStreamCloses();

    [Fact] void should_keep_checking() => _checks.ShouldEqual(ChecksBeforeTheStreamCloses);
    [Fact] void should_complete_when_the_stream_closes() => _completed.ShouldBeTrue();
}
