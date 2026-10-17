// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Resolves the host's identity without inventing a logical activation on each instance.
/// </summary>
internal static class LifecycleActivation
{
    static readonly ActivationId _process = new(Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Resolves the explicitly configured identity or the opt-in per-process identity.
    /// </summary>
    /// <param name="options">The lifecycle configuration.</param>
    /// <returns>The identity reused through startup, reconnect and shutdown.</returns>
    /// <exception cref="ActivationIdRequired">No valid identity policy was supplied.</exception>
    internal static ActivationId Resolve(ChronicleOptions options)
    {
        if (options.ActivationScope == ActivationScope.PerProcess) return _process;
        if (options.ActivationId is { } id && !string.IsNullOrWhiteSpace(id.Value)) return id;
        throw new ActivationIdRequired();
    }
}
