// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents bearer token authentication.
/// </summary>
/// <param name="Token">The bearer token.</param>
public record BearerTokenAuthorization(string Token);
