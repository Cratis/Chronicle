// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection;

[DependencyInjection.IgnoreConvention]
public class AReactor : IReactor
{
    public Task OnAnEventHappened(AnEventHappened evt, EventContext ctx) => Task.CompletedTask;
    public Task OnAnotherEventHappened(AnotherEventHappened evt, EventContext ctx) => Task.CompletedTask;
}
