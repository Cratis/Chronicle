// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// A total belonging to exactly one event source.
/// </summary>
/// <param name="Id">The event source key.</param>
/// <param name="Total">The sum of that source's contributions.</param>
public record IsolatedContentMutationDocument(string Id, int Total);
