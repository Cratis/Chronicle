// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Shared.Events;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
/// <param name="Metadata">The <see cref="EventMetadata"/>.</param>
/// <param name="Context">The <see cref="EventContext"/>.</param>
/// <param name="Content">The content in the form of an <see cref="ExpandoObject"/>.</param>
public record AppendedEvent(EventMetadata Metadata, EventContext Context, ExpandoObject Content);
