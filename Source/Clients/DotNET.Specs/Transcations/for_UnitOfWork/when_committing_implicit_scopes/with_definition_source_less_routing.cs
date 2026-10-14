// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_definition_source_less_routing : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        _eventSources.GetFor(Cart.ClrType).Returns(Cart with { Streams = [new EventStream("Payment", string.Empty, ConcurrencyDimensions.EventStreamType)] });
        Guard();
        _enrollment.AddEvents(EventSequenceId.Log, [Payment()], []);
    }
    Task Because() => Commit();
    [Fact] void should_send_the_source_wide_guard() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
    [Fact] void should_preserve_the_definition_guard() => Assert.Equivalent(new ConcurrencyScope(10, EventStreamType: "Payment").ToSequencesContract(), ScopeFor($"$keyguard:routing:{_sourceId.Value}"));
}
