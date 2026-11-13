// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Imports;

[Reactor("imported-customers")]
public class ImportedCustomersReactor : IReactor
{
    public static readonly ConcurrentQueue<(string Name, EventSequenceNumber SequenceNumber)> Handled = new();

    public Task Imported(CustomerImported @event, EventContext context)
    {
        Handled.Enqueue((@event.Name, context.SequenceNumber));
        return Task.CompletedTask;
    }
}
