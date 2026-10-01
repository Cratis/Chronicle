// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class AccumulatingReducer : IReducerFor<SomeReadModel>
{
    public int Multiplier = 1;

    public SomeReadModel OnSomeEvent(SomeEvent evt, SomeReadModel? input) =>
        new((input?.Number ?? 0) + (evt.Number * Multiplier));
}
