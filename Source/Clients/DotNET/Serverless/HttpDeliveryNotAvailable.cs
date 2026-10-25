// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Exception that gets thrown when an observer resolves to HTTP delivery, which this client cannot register yet.
/// </summary>
/// <remarks>
/// A later version replaces this with a check of what the kernel supports.
/// </remarks>
/// <param name="observerIds">The identifiers of the observers that resolve to HTTP delivery.</param>
public class HttpDeliveryNotAvailable(IEnumerable<string> observerIds)
    : Exception($"Observers [{string.Join(", ", observerIds)}] are delivered over HTTP, but this client version cannot register HTTP-delivered observers yet. Remove [Delivery(ObserverDelivery.Http)] or the Cratis:Chronicle:Serverless:Observers override, or set Cratis:Chronicle:Serverless:DefaultDelivery to Stream, which skips them in serverless mode.");
