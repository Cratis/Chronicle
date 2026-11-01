// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Event appended by <see cref="LargeBatchAppendBenchmark"/> to measure the cost personal data adds to an append:
/// every subject the batch introduces gets an encryption key. Like <see cref="AppendOnlyEvent"/> it has no observer.
/// </summary>
/// <param name="Name">The person's name, which is personal data.</param>
/// <param name="Value">The benchmark value.</param>
[EventType]
public record PersonRegistered([property: PII] string Name, int Value);
