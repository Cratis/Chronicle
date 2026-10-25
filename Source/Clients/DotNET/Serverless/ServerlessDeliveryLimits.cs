// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Represents the local limits applied when receiving deliveries.
/// </summary>
public class ServerlessDeliveryLimits
{
    /// <summary>
    /// Gets or sets the maximum request body size in bytes that the receiver reads. Larger deliveries are refused before any handler runs.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core's own server limit applies first; Kestrel defaults to approximately 30 MB.
    /// </remarks>
    public long MaximumBodySize { get; set; } = 16 * 1024 * 1024;
}
