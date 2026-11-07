// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Represents the settings for how the kernel delivers events to the receiver.
/// </summary>
/// <remarks>
/// Bound from <c language="json">Cratis:Chronicle:Serverless:Delivery</c>. These settings have no effect until HTTP delivery is available.
/// </remarks>
[Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
public class ServerlessDeliveryOptions
{
    /// <summary>
    /// Gets or sets how long the kernel waits for the receiver to answer one HTTP reactor or reducer delivery. Defaults to 30 seconds.
    /// </summary>
    /// <remarks>
    /// Applies to HTTP reactors and reducers only, and takes precedence over the kernel's webhook request timeout (60 seconds), which
    /// keeps governing webhook observers. It must be greater than zero.
    /// </remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
