// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;

namespace Orleans;

public interface IOrder : IAggregateRoot
{
    Task DoStuff();
    Task DoOtherStuff();
}
