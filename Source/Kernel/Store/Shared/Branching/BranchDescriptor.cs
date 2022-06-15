// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Branching;

/// <summary>
/// Represents a descriptor of a branch.
/// </summary>
/// <param name="Type">Type of branch.</param>
/// <param name="Identifier">Identifier identifying the branch uniquely.</param>
/// <param name="tags">Any tags associated with the branch.</param>
public record BranchDescriptor(BranchTypeId Type, BranchId Identifier, IDictionary<string, string> tags);
