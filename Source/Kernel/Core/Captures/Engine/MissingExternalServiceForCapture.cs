// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// The exception that is thrown when a capture references an external service that is not registered.
/// </summary>
/// <param name="name">The name of the external service the capture references.</param>
public class MissingExternalServiceForCapture(string name)
    : Exception($"There is no external service named '{name}'");
