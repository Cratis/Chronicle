// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents a specialized version of <see cref="AppendedEvent"/> used in specifications.
/// </summary>
/// <param name="Metadata">The <see cref="EventMetadata"/>.</param>
/// <param name="Context">The <see cref="EventContext"/>.</param>
/// <param name="Content">The content in the form of JSON.</param>
/// <param name="ActualEvent">The actual original event committed.</param>
public record AppendedEventForSpecifications(
    EventMetadata Metadata,
    EventContext Context,
    JsonObject Content,
    object ActualEvent) : AppendedEvent(Metadata, Context, Content);
