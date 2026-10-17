// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamIndexUpdater.when_updating;

public class and_the_write_is_cancelled : given.a_closing_index
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => new ClosesStreamIndexUpdater(_definition, ContextFor("Closed"), _storage).Update(0, new CancellationToken(canceled: true)));

    [Fact] void should_propagate_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] async Task should_not_write_a_closure() => (await _storage.GetForOwner("closing")).ShouldBeEmpty();
}
