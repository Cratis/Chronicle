// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a MongoDB document for a closed stream entry.
/// </summary>
/// <param name="StreamType">The type of the stream.</param>
/// <param name="StreamId">The identifier of the stream.</param>
public record ClosedStreamDocument(string StreamType, string StreamId);
