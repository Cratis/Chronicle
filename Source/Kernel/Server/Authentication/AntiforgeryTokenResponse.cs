// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents an antiforgery request token issued to the authenticated Workbench.
/// </summary>
/// <param name="RequestToken">The token to send in the antiforgery request header.</param>
public record AntiforgeryTokenResponse(string RequestToken);
