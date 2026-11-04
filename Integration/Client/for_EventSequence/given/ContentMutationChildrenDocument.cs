// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// A shared parent containing children contributed by different event sources.
/// </summary>
/// <param name="Id">The parent document key.</param>
/// <param name="Items">The contributing children.</param>
public record ContentMutationChildrenDocument(string Id, IEnumerable<ContentMutationChild> Items);
