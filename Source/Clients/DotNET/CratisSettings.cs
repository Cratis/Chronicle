// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;

namespace Aksio.Cratis;

/// <summary>
/// Represents the settings for connecting to Cratis.
/// </summary>
public class CratisSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CratisSettings"/> class.
    /// </summary>
    /// <param name="url"><see cref="CratisUrl"/> to use.</param>
    /// <param name="kernelConnectivity"><see cref="KernelConnectivity"/> to use.</param>
    public CratisSettings(CratisUrl url, KernelConnectivity kernelConnectivity)
    {
        Url = url;
        KernelConnectivity = kernelConnectivity;
    }

    /// <summary>
    /// Gets the <see cref="CratisUrl"/> to use.
    /// </summary>
    public CratisUrl Url { get; init; }

    /// <summary>
    /// Gets the <see cref="KernelConnectivity"/> to use.
    /// </summary>
    public KernelConnectivity KernelConnectivity { get; init; }

    /// <summary>
    /// Create a <see cref="CratisSettings"/> from a connection string.
    /// </summary>
    /// <param name="connectionString">Connection string to create from.</param>
    /// <returns>A new <see cref="CratisSettings"/>.</returns>
    public static CratisSettings FromConnectionString(string connectionString) => FromUrl(new CratisUrl(connectionString));

    /// <summary>
    /// Create a <see cref="CratisSettings"/> from a <see cref="CratisUrl"/>.
    /// </summary>
    /// <param name="url"><see cref="CratisUrl"/> to create from.</param>
    /// <returns>A new <see cref="CratisSettings"/>.</returns>
    public static CratisSettings FromUrl(CratisUrl url) =>
        new(url, new KernelConnectivity
        {
            SingleKernel = new SingleKernelOptions()
        });
}
