// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents basic authentication for a capture source.
/// </summary>
/// <param name="Username">The username.</param>
/// <param name="Password">The password.</param>
public record SourceBasicAuthorization(string Username, string Password);
