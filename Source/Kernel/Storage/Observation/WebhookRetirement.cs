// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>Durable terminal endpoint episode, without credentials, path, query or response data.</summary>
/// <param name="Id">The stable incident identity.</param>
/// <param name="Occurred">When the endpoint returned Gone.</param>
/// <param name="Authority">The sanitized target authority, without user information.</param>
public record WebhookRetirement(Guid Id, DateTimeOffset Occurred, string Authority);
