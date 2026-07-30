// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// The exception that is thrown when a capture declaration uses a capability the capturing engine does not support yet.
/// </summary>
/// <param name="message">Message describing the unsupported capability.</param>
public class UnsupportedCaptureCapability(string message) : Exception(message);
