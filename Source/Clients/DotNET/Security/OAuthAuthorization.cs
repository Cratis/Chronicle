// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents OAuth authentication for a webhook.
/// </summary>
/// <param name="Authority">The OAuth authority.</param>
/// <param name="ClientId">The OAuth client ID.</param>
/// <param name="ClientSecret">The OAuth client secret.</param>
public record OAuthAuthorization(string Authority, string ClientId, string ClientSecret);
