// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.InMemory;

/// <summary>
/// Represents a <see cref="IProjectionSinkRewindScope"/> for in-memory.
/// </summary>
public class InMemoryProjectionSinkRewindScope : IProjectionSinkRewindScope
{
    /// <inheritdoc/>
    public Model Model { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryProjectionSinkRewindScope"/> class.
    /// </summary>
    /// <param name="model"><see cref="Model"/> the scope is for.</param>
    public InMemoryProjectionSinkRewindScope(Model model) => Model = model;

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
