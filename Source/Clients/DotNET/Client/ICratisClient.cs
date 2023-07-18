// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Client;

/// <summary>
/// Defines the client for Cratis.
/// </summary>
public interface ICratisClient
{
    /// <summary>
    /// Connect to Cratis kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Connect();
}
