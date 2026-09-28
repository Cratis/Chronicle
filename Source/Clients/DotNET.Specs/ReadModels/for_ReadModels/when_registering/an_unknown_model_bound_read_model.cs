// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class an_unknown_model_bound_read_model : for_ReadModels.given.all_dependencies
{
    [EventType]
    record CustomerRegistered(string Name);

    [FromEvent<CustomerRegistered>]
    record Customer(string Name);

    async Task Because() => await _readModels.Register<Customer>();

    [Fact] void should_register_its_model_bound_projection_explicitly() => _projections.Received(1).Register<Customer>();
    [Fact] void should_leave_registering_the_read_model_to_the_projection() => _services.ReadModels.DidNotReceive().RegisterMany(Arg.Any<Contracts.ReadModels.RegisterManyRequest>());
}
