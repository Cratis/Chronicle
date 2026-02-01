// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents basic authentication for a webhook.
/// </summary>
/// <param name="Username">The username.</param>
/// <param name="Password">The password.</param>
public record BasicAuthorization(string Username, string Password);
