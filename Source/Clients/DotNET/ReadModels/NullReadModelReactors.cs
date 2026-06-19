// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents a no-op implementation of <see cref="IReadModelReactors"/> for testing scenarios where read
/// model reactors are not exercised.
/// </summary>
public class NullReadModelReactors : IReadModelReactors
{
    /// <inheritdoc/>
    public void Start()
    {
    }

    /// <inheritdoc/>
    public void Stop()
    {
    }

    /// <inheritdoc/>
    public void Dispose() => GC.SuppressFinalize(this);
}
