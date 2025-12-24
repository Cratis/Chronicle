// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for adding application.
/// </summary>
/// <param name="Id">The application identifier.</param>
/// <param name="ClientId">The application's client identifier.</param>
/// <param name="ClientSecret">The application's client secret.</param>
public record AddApplication(
    string Id,
    string ClientId,
    string ClientSecret);
