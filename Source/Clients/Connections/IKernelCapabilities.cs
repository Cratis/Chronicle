// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides the optional behaviors advertised by a connected kernel.
/// </summary>
/// <remarks>
/// Connections that do not implement this contract are treated as having no advertised capabilities.
/// </remarks>
public interface IKernelCapabilities
{
    /// <summary>
    /// Gets the behaviors advertised by the current kernel. Older kernels advertise none.
    /// </summary>
    IReadOnlyCollection<string> Capabilities { get; }
}
