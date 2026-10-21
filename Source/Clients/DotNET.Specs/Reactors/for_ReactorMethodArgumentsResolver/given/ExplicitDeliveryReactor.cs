// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.given;

[EventSequence("custom")]
public class ExplicitDeliveryReactor : IReactor
{
    public Task Handle(ExternalEvent @event, ReactorDelivery delivery) => Task.CompletedTask;
}
