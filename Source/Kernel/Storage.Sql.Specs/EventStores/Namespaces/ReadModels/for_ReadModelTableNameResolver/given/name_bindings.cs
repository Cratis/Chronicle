// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.given;

internal sealed class name_bindings(name_catalog catalog) : IReadModelTableNameBindings
{
    internal readonly List<ReadModelTableNameBinding> Rows = [];
    internal Action<ReadModelTableNameBinding>? BeforeInsert;
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<ReadModelTableNameBinding> UnderLock(Func<Task<ReadModelTableNameBinding>> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<ReadModelTableNameBinding?> Find(string id) => Task.FromResult(Rows.SingleOrDefault(row => row.Id == id));
    public Task<IReadOnlyList<ReadModelTableNameBinding>> Read() => Task.FromResult<IReadOnlyList<ReadModelTableNameBinding>>(Rows.ToArray());

    public async Task<bool> TryInsert(ReadModelTableNameBinding binding)
    {
        BeforeInsert?.Invoke(binding);
        foreach (var row in Rows)
        {
            if (row.Id == binding.Id || await catalog.Equal(row.PhysicalName, binding.PhysicalName))
            {
                return false;
            }
        }
        Rows.Add(binding);
        return true;
    }
}
