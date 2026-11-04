// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the shared reservation protocol: insert each key, and on a duplicate compare the owner, rolling back on a different one.
/// </summary>
/// <remarks>
/// The uniqueness of a key is enforced by the store a derived class writes to, which is what makes the reservation atomic across silos.
/// </remarks>
public abstract class ResourceNameReservations : IResourceNameReservations
{
    /// <inheritdoc/>
    public async Task<bool> Reserve(string owner, IReadOnlyCollection<string> keys)
    {
        var inserted = new List<string>();
        try
        {
            foreach (var key in keys)
            {
                var existingOwner = await TryAdd(key, owner);
                if (existingOwner is null)
                {
                    inserted.Add(key);
                    continue;
                }

                if (!string.Equals(existingOwner, owner, StringComparison.Ordinal))
                {
                    await Remove(inserted, owner);
                    return false;
                }
            }
        }
        catch
        {
            await Remove(inserted, owner);
            throw;
        }

        return true;
    }

    /// <summary>
    /// Insert a key for an owner, unless the key exists.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="owner">The owner.</param>
    /// <returns>Null when the key was inserted, otherwise the owner already holding it.</returns>
    protected abstract Task<string?> TryAdd(string key, string owner);

    /// <summary>
    /// Remove keys that an owner inserted.
    /// </summary>
    /// <param name="keys">The keys.</param>
    /// <param name="owner">The owner that holds them.</param>
    /// <returns>Awaitable task.</returns>
    protected abstract Task Remove(IReadOnlyCollection<string> keys, string owner);
}
