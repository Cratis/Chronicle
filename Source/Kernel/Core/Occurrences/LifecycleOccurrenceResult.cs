// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Reports durable lifecycle recording and the original namespace snapshot.
/// </summary>
/// <param name="Id">The application occurrence or fan-out intent identity.</param>
/// <param name="Status">The durable ingress outcome.</param>
/// <param name="Namespaces">The fixed namespace snapshot, empty under application scope.</param>
[ReadModel]
public record LifecycleOccurrenceResult(string Id, OccurrenceIngressStatus Status, IEnumerable<string> Namespaces);
