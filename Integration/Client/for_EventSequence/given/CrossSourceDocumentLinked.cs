// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// Links a materialized document to a reference owned by another event source.
/// </summary>
/// <param name="DocumentId">The materialized document key.</param>
/// <param name="ReferenceId">The reference event source.</param>
[EventType]
public record CrossSourceDocumentLinked(string DocumentId, string ReferenceId);
