// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors;

[DependencyInjection.IgnoreConvention]
[FilterEventsByTag("audited")]
public class ReactorCapturingAuditedEvents : IReactor
{
    public ConcurrentQueue<int> Numbers { get; } = new();

    public Task OnSomeEvent(SomeEvent evt)
    {
        Numbers.Enqueue(evt.Number);
        return Task.CompletedTask;
    }
}
