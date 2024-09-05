// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;

namespace Orleans;

public class Order : AggregateRoot<OrderState>, IOrder
{
    public async Task DoStuff()
    {
        Console.WriteLine($"Before : {State?.CartItems?.Count()}");

        await Apply(new ItemAddedToCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid()),
            1,
            null,
            null));

        Console.WriteLine($"After : {State?.CartItems?.Count()}");
    }

    public async Task AddItem(MaterialId materialId)
    {
        await Apply(new ItemAddedToCart(
            new(Guid.NewGuid()),
            materialId,
            1,
            null,
            null));
    }

    public async Task DoOtherStuff()
    {
        await Apply(new ItemRemovedFromCart(
            new(Guid.NewGuid()),
            new(Guid.NewGuid())));
    }
}
