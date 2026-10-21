// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A bounded page whose cursor remains valid when previously consumed rows are deleted.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
/// <param name="Rows">The rows in this page.</param>
/// <param name="Next">The opaque exclusive continuation cursor, or null at the end.</param>
public record ObserverDefinitionChangePage<T>(IReadOnlyList<ObserverDefinitionChangeRow<T>> Rows, string? Next);
