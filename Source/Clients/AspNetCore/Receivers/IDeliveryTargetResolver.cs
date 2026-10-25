// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Receivers;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Receivers;

/// <summary>
/// Resolves the legacy delivery target supplied by the transport.
/// </summary>
internal interface IDeliveryTargetResolver
{
    /// <summary>
    /// Resolves a target without reading the body.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The target, or null for invalid route values.</returns>
    DeliveryTarget? Resolve(HttpContext context);
}
