// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// The latest contribution from one source within a shared parent.
/// </summary>
/// <param name="Id">The contributing event source.</param>
/// <param name="Amount">Its latest amount.</param>
public record ContentMutationChild(string Id, int Amount);
