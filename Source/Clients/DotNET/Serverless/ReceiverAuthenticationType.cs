// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Defines how a receiver authenticates deliveries from the kernel.
/// </summary>
[Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
public enum ReceiverAuthenticationType
{
    /// <summary>
    /// Deliveries are signed by the kernel and the receiver verifies the signature. This is the default.
    /// </summary>
    Signature = 0,

    /// <summary>
    /// Deliveries are not authenticated. Requires <see cref="ServerlessAuthOptions.AllowUnauthenticated"/>.
    /// </summary>
    None = 1
}
