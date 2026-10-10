// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_metadata_at;

/// <summary>
/// An event whose payload is not needed by a metadata reader.
/// </summary>
/// <param name="Value">The event value.</param>
[EventType]
public record MetadataRecorded(string Value);
