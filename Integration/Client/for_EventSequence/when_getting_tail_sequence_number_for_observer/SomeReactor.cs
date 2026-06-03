// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_tail_sequence_number_for_observer;

[DependencyInjection.IgnoreConvention]
public class SomeReactor : IReactor
{
    public Task OnSomeEvent(SomeEvent evt, EventContext ctx) => Task.CompletedTask;
}
