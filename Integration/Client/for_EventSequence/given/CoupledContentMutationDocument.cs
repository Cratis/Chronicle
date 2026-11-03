// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// A total shared by contributions from several event sources.
/// </summary>
/// <param name="Id">The document key.</param>
/// <param name="Total">The sum of its contributions.</param>
public record CoupledContentMutationDocument(string Id, int Total);
