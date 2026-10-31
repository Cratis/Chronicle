// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Separates namespace delivery from event-store application storage.
/// </summary>
/// <param name="ApplicationScope">Whether this is the application queue.</param>
/// <param name="Namespace">The namespace when not application scoped.</param>
internal record OccurrenceDeliveryScope(bool ApplicationScope, string? Namespace);
