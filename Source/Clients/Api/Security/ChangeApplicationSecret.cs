// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for changing application secret.
/// </summary>
/// <param name="Id">The application identifier.</param>
/// <param name="ClientSecret">The new client secret.</param>
public record ChangeApplicationSecret(
    string Id,
    string ClientSecret);
