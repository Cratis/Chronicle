// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Fixes a lifecycle occurrence and its namespace snapshot before any namespace is written.
/// </summary>
/// <param name="ActivationId">The logical activation identity.</param>
/// <param name="Occurrence">The immutable occurrence template.</param>
/// <param name="Namespaces">The namespaces existing at recording time, in fixed order.</param>
/// <param name="Progress">The number of namespace writes durably acknowledged.</param>
public record LifecycleFanOutIntent(string ActivationId, Occurrence Occurrence, IReadOnlyList<string> Namespaces, int Progress = 0);
