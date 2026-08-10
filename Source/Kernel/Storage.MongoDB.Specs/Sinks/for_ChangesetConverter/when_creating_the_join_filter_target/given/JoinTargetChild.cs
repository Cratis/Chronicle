// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target.given;

/// <summary>
/// A child identified by a Guid-backed column, so the child branch of the join filter can be specified
/// against the same conversion the root branch performs.
/// </summary>
public class JoinTargetChild
{
    public Guid ChildId { get; set; }

    public string? Name { get; set; }
}
