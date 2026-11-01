// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// The exception that is thrown when a capture route leaves its external service's origin.
/// </summary>
/// <param name="externalService">The name of the external service the capture references.</param>
public class CaptureRouteOutsideExternalService(string externalService)
    : Exception($"The capture route for external service '{externalService}' must resolve to that service's own scheme, host and port.");
