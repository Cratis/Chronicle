// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Rules;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Orleans;

[Route("/api/orders")]
public class Orders(IAggregateRootFactory aggregateRootFactory) : ControllerBase
{
    [HttpPost("items")]
    public async Task AddItem([FromBody] AddItemToOrder command)
    {
        var aggregateRoot = await aggregateRootFactory.Get<IOrder>("6fbd1b71-923d-4fa7-bf44-777dcb091218");
        await aggregateRoot.DoStuff();
    }
}

public record AddItemToOrder();

public class AddItemToOrderRules : RulesFor<AddItemToOrderRules, AddItemToOrder>
{
    public int Count { get; set; }

    public AddItemToOrderRules()
    {
        RuleForState(_ => _.Count)
            .LessThan(5)
            .WithMessage("Quantity can't exceed 5");
    }

    public override void DefineState(IProjectionBuilderFor<AddItemToOrderRules> builder) => builder
        .From<ItemAddedToCart>(_ => _.Count(_ => _.Count));
}
