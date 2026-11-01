// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Retains type descriptors only; handlers, activation and ownership validation remain per store.
/// </summary>
internal sealed class CachedReducerDescriptorsSource : IReducerDescriptorsSource
{
    readonly object _sync = new();
    ImmutableArray<ReducerDescriptor> _descriptors;

    /// <summary>
    /// Gets the number of successful descriptor builds.
    /// </summary>
    internal int BuildCount { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<ReducerDescriptor> Get(Func<IReadOnlyList<ReducerDescriptor>> discover)
    {
        lock (_sync)
        {
            if (_descriptors.IsDefault)
            {
                _descriptors = discover().ToImmutableArray();
                BuildCount++;
            }
            return _descriptors;
        }
    }
}
