// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.given;

[Reactor("epoched-delivery-reactor", effectEpoch: 5)]
public class EpochedDeliveryReactor : IReactor
{
    public Task Handle(ExternalEvent @event, ReactorDelivery delivery) => Task.CompletedTask;
}
