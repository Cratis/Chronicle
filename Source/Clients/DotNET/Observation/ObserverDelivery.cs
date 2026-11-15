// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines how the events of an observer are delivered to the application.
/// </summary>
[Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
public enum ObserverDelivery
{
    /// <summary>
    /// The kernel delivers events over a long-lived stream the client keeps open. Needs a long-running process.
    /// </summary>
    Stream = 0,

    /// <summary>
    /// The kernel delivers events by calling an HTTP endpoint hosted by the application.
    /// </summary>
    /// <remarks>
    /// HTTP delivery is not available in this version of the client yet. An observer that resolves to it fails
    /// at startup with <see cref="Cratis.Chronicle.Serverless.HttpDeliveryNotAvailable"/>.
    /// </remarks>
    Http = 1
}
