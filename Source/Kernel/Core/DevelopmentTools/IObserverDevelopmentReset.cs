// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.DevelopmentTools;

/// <summary>
/// Defines the development-only reset boundary for a KeepAlive observer activation.
/// </summary>
public interface IObserverDevelopmentReset : IGrainWithStringKey
{
    /// <summary>
    /// Unsubscribes and seals the activation against further state writes before scheduling deactivation.
    /// </summary>
    /// <returns>A task representing completion of the reset.</returns>
    /// <remarks>Only for development and integration storage wipes; does not remove persisted observer definitions.</remarks>
    Task ResetForDevelopment();
}
