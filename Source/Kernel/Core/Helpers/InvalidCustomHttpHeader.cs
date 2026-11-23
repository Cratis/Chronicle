// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers;

/// <summary>
/// The exception thrown for an invalid custom HTTP header, without exposing its value.
/// </summary>
/// <param name="name">The header name.</param>
public class InvalidCustomHttpHeader(string name)
    : Exception($"Custom HTTP header '{name}' has an invalid name or value.");
