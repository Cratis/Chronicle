// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// The exception that is thrown when the state of a read model identified by a flat composite key contradicts the key.
/// </summary>
/// <param name="part">The key part that contradicts the key.</param>
/// <param name="expected">The value the key gives the part.</param>
/// <param name="actual">The value the state has for the part.</param>
public class FlatCompositeKeyIdentityViolated(string part, object? expected, object? actual)
    : Exception($"The read model is identified by a flat composite key, but its property '{part}' is '{actual}' where the key says '{expected}'. A projection cannot change, remove or contradict a part of the identity of the read model it projects.")
{
    /// <summary>
    /// Gets the key part that contradicts the key.
    /// </summary>
    public string Part { get; } = part;
}
