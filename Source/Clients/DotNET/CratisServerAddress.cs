// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis;

/// <summary>
/// Represents a server address for Cratis.
/// </summary>
/// <param name="Host">Host name where Cratis Kernel is running.</param>
/// <param name="Port">The port in which Cratis Kernel is exposed on, defaults to 35000.</param>
public record CratisServerAddress(string Host, int Port = 35000);
