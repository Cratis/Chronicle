// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class NullableCounterReducer : IReducerFor<NullableCounter>
{
    public NullableCounter Reduce(SomeEvent @event, NullableCounter? current) =>
        new((current?.Count ?? 0) + @event.Number, null);
}
