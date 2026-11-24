// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the payload for an identity.
/// </summary>
/// <param name="Subject">The subject of the identity.</param>
/// <param name="Name">The name of the identity.</param>
/// <param name="UserName">The username of the identity.</param>
/// <param name="OnBehalfOf">The identity on behalf of which the event was created.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Sequences.Identity")]
public record Identity(
    [Id(0)] string Subject,
    [Id(1)] string Name,
    [Id(2)] string UserName,
    [Id(3)] Identity? OnBehalfOf);
