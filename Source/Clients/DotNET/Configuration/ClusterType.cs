// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

#pragma warning disable CA1720 // Allow single, which is also a typename

/// <summary>
/// Represents types of clusters the client supports.
/// </summary>
public enum ClusterType
{
    /// <summary>
    /// Single cluster mode, typically used in a local development scenario.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Static cluster mode with a static well known collection of Kernel instances.
    /// </summary>
    Static = 2,

    /// <summary>
    /// Azure Storage based cluster mode that is based upon the Orleans registered instances.
    /// </summary>
    AzureStorage = 3
}
