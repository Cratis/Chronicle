// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.given;

public class a_stream_model : all_dependencies
{
    [EventSource("BankAccount")]
    [EventSources.EventStream("Transactions")]
    protected class BankAccount : IEventSource;

    [EventStream<BankAccount>("Transactions")]
    protected record MonthlyState(string Id);

    void Establish()
    {
        var sources = Substitute.For<IEventSources>();
        sources.GetFor(typeof(BankAccount)).Returns(EventSources.EventSources.Describe(typeof(BankAccount)));
        _eventStore.EventSources.Returns(sources);
#pragma warning disable CA2263 // Configure the runtime-type overload used by the reader.
        _projections.HasFor(typeof(MonthlyState)).Returns(true);
#pragma warning restore CA2263
    }
}
