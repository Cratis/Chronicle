// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the different cluster types.
/// </summary>
public static class ClusterTypes
{
    /// <summary>
    /// Gets the value representing single cluster mode.
    /// </summary>
    #pragma warning disable CA1720  // Allow single as name
    public const string Single = "single";
    #pragma warning restore

    /// <summary>
    /// Gets the value representing an unreliable static cluster configuration.
    /// </summary>
    public const string Static = "static";

    /// <summary>
    /// Gets the value representing a cluster based on Azure Storage.
    /// </summary>
    public const string AzureStorage = "azure-storage";

    /// <summary>
    /// Gets the value representing a cluster based on ADO .NET Storage.
    /// </summary>
    public const string AdoNet = "ado-net";
}
