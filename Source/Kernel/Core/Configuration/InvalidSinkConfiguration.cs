// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// The exception that is thrown when the kernel's <c language="text">sinks</c> configuration is invalid.
/// </summary>
/// <param name="name">The name of the configuration, or the setting, that is invalid.</param>
/// <param name="reason">Why it is invalid.</param>
public class InvalidSinkConfiguration(string name, string reason) : Exception($"Sink configuration '{name}' is invalid: {reason}");
