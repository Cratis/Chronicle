// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Represents the authentication settings for the receiver of HTTP deliveries.
/// </summary>
[Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
public class ServerlessAuthOptions
{
    /// <summary>
    /// Gets or sets how deliveries are authenticated. Defaults to <see cref="ReceiverAuthenticationType.Signature"/>.
    /// </summary>
    public ReceiverAuthenticationType Type { get; set; } = ReceiverAuthenticationType.Signature;

    /// <summary>
    /// Gets or sets a value indicating whether the receiver may run without authenticating deliveries.
    /// </summary>
    /// <remarks>
    /// Must be set explicitly when <see cref="Type"/> is <see cref="ReceiverAuthenticationType.None"/>.
    /// </remarks>
    public bool AllowUnauthenticated { get; set; }
}
