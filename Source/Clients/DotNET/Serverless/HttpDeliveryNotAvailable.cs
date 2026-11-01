// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Exception that gets thrown when an observer resolves to HTTP delivery, which this client cannot register yet.
/// </summary>
/// <remarks>
/// The client fails closed: it does not register HTTP-delivered observers whatever the kernel advertises. Lifting this takes all of:
/// <list type="number">
/// <item>The kernel advertises an HTTP-delivery capability (added to <c language="csharp">KernelCapabilities</c> by #5116), and the client requires
/// <c language="csharp">CapabilitiesAreKnown</c> together with that capability.</item>
/// <item>The kernel drains replay and recovery state before an observer switches from stream to HTTP delivery (#5115, decision 0018).</item>
/// <item>The receiver exists (#5119).</item>
/// </list>
/// </remarks>
/// <param name="observerIds">The identifiers of the observers that resolve to HTTP delivery.</param>
[Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
public class HttpDeliveryNotAvailable(IEnumerable<string> observerIds)
    : Exception($"Observers [{string.Join(", ", observerIds)}] are delivered over HTTP, but this client version cannot register HTTP-delivered observers yet. Remove [Delivery(ObserverDelivery.Http)] or the Cratis:Chronicle:Serverless:Observers override, or set Cratis:Chronicle:Serverless:DefaultDelivery to Stream, which skips them in serverless mode.");
