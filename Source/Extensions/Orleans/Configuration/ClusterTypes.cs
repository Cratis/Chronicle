// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents the different cluster types.
/// </summary>
public static class ClusterTypes
{
    /// <summary>
    /// Gets the value representing local cluster mode.
    /// </summary>
    public const string Local = "local";

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
