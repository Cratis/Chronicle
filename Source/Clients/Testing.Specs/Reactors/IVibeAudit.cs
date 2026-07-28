// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A service the <see cref="VibeCancellationReactor"/> takes as a handler-method parameter, used to verify that
/// service-typed method parameters are resolved from the scenario's service provider.
/// </summary>
public interface IVibeAudit
{
    /// <summary>
    /// Records that a vibe hosted by the given host was cancelled.
    /// </summary>
    /// <param name="host">The host of the cancelled vibe.</param>
    void Record(string host);
}
