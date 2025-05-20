// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Identities;

/// <summary>
/// Represents the payload for an identity.
/// </summary>
/// <param name="Subject">The subject of the identity.</param>
/// <param name="Name">The name of the identity.</param>
/// <param name="UserName">The username of the identity.</param>
/// <param name="OnBehalfOf">The identity on behalf of which the event was created.</param>
public record Identity(
    string Subject,
    string Name,
    string UserName,
    Identity? OnBehalfOf);
