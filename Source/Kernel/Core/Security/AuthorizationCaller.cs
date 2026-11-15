// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security;

/// <summary>The caller and its service provider for a single decision.</summary>
/// <param name="Principal">The caller.</param>
/// <param name="Services">The caller's provider.</param>
internal readonly record struct AuthorizationCaller(ClaimsPrincipal? Principal, IServiceProvider? Services);
