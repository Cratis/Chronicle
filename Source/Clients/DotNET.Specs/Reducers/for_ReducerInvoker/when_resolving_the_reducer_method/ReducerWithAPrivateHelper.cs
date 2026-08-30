// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_resolving_the_reducer_method;

/// <summary>
/// A reducer whose public method delegates to a helper with no access modifier - so private, and shaped
/// exactly like a reducer method. Dispatching to the helper instead would leave the count two short.
/// </summary>
public class ReducerWithAPrivateHelper
{
    public ReadModel Reduce(ValidEvent @event, ReadModel? current, EventContext context) => new(Apply(@event, current).Count + 2);

    ReadModel Apply(ValidEvent @event, ReadModel? current) => new((current?.Count ?? 0) + 1);
}
