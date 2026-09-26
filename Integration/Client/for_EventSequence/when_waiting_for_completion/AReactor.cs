// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion;

[DependencyInjection.IgnoreConvention]
public class AReactor : IReactor
{
    public Task OnARecorded(ARecorded @event) => Task.CompletedTask;
}
