// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.when_a_client_responds;

/// <summary>
/// A client that stopped responding for the stale threshold was dropped on purpose; it starting to respond again on the
/// same connection does not undo that.
/// </summary>
public class and_it_had_stopped_responding_long_enough_to_be_dropped : given.an_open_connection
{
    bool _restoredAfterTheGap;
    bool _restoredWhileRespondingAgain;

    void Because()
    {
        Responded(registryKnowsClient: true, TimeSpan.FromSeconds(1));
        _restoredAfterTheGap = Responded(registryKnowsClient: false, TimeSpan.FromSeconds(7));
        _restoredWhileRespondingAgain = Responded(registryKnowsClient: false, TimeSpan.FromSeconds(8));
    }

    [Fact] void should_not_restore_it_after_the_gap() => _restoredAfterTheGap.ShouldBeFalse();
    [Fact] void should_not_restore_it_once_it_responds_again() => _restoredWhileRespondingAgain.ShouldBeFalse();
}
