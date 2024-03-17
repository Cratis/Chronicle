// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Reflection;

namespace Cratis.Types;

/// <summary>
/// Represents an implementation of <see cref="IContractToImplementorsMap"/>.
/// </summary>
public class ContractToImplementorsMap : IContractToImplementorsMap
{
    readonly ConcurrentDictionary<Type, ConcurrentBag<Type>> _contractsAndImplementors = new();
    readonly ConcurrentDictionary<Type, Type> _allTypes = new();

    /// <inheritdoc/>
    public IDictionary<Type, IEnumerable<Type>> ContractsAndImplementors => _contractsAndImplementors.ToDictionary(_ => _.Key, _ => _.Value.AsEnumerable());

    /// <inheritdoc/>
    public IEnumerable<Type> All => _allTypes.Keys;

    /// <inheritdoc/>
    public void Feed(IEnumerable<Type> types)
    {
        MapTypes(types);
        AddTypesToAllTypes(types);
    }

    /// <inheritdoc/>
    public IEnumerable<Type> GetImplementorsFor<T>()
    {
        return GetImplementorsFor(typeof(T));
    }

    /// <inheritdoc/>
    public IEnumerable<Type> GetImplementorsFor(Type contract)
    {
        return GetImplementingTypesFor(contract);
    }

    void AddTypesToAllTypes(IEnumerable<Type> types)
    {
        foreach (var type in types) _allTypes[type] = type;
    }

    void MapTypes(IEnumerable<Type> types)
    {
        var implementors = types.Where(IsImplementation);
        Parallel.ForEach(implementors, implementor =>
        {
            foreach (var contract in implementor.AllBaseAndImplementingTypes())
            {
                var implementingTypes = GetImplementingTypesFor(contract);
                if (!implementingTypes.Contains(implementor)) implementingTypes.Add(implementor);
            }
        });
    }

    bool IsImplementation(Type type) => !type.IsInterface && !type.IsAbstract;

    ConcurrentBag<Type> GetImplementingTypesFor(Type contract)
    {
        return _contractsAndImplementors.GetOrAdd(contract, _ => new ConcurrentBag<Type>());
    }
}
