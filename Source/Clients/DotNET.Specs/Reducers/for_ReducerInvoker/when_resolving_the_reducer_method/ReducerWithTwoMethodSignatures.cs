// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_resolving_the_reducer_method;

public class ReducerWithTwoMethodSignatures
{
    public ReadModel ReduceWithContext(ValidEvent @event, ReadModel? current, EventContext context) => new((current?.Count ?? 0) + 10);

    public ReadModel Reduce(ValidEvent @event, ReadModel? current) => new((current?.Count ?? 0) + 1);
}
