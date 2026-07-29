// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Exception that gets thrown when a capture is built without configuring a source.
/// </summary>
public class MissingCaptureSourceConfiguration()
    : Exception("A capture source must be configured before building the capture definition.");
