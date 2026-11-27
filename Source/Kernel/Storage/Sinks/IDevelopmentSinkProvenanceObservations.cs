// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Rotates observations after successful destructive development storage reset.
/// </summary>
public interface IDevelopmentSinkProvenanceObservations
{
    /// <summary>
    /// Revokes and drains the old observation generation without removing durable restrictions.
    /// </summary>
    /// <returns>The rotation task.</returns>
    Task RotateAfterWipe();
}
#endif
