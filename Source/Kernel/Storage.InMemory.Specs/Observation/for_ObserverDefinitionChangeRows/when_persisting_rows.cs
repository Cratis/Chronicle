// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionChangeRows;

public class when_persisting_rows : DefinitionChangeRowsContract
{
    readonly Dictionary<Type, object> _stores = [];

    protected override IObserverDefinitionChangeRows<T> OpenRows<T>()
    {
        if (!_stores.TryGetValue(typeof(T), out var store))
        {
            store = new ObserverDefinitionChangeRows<T>();
            _stores.Add(typeof(T), store);
        }
        return (IObserverDefinitionChangeRows<T>)store;
    }
}
