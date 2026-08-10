// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target.given;

/// <summary>
/// A nested object, so a join declared on a path rather than a top-level column can be specified.
/// </summary>
public class JoinTargetNested
{
    public Guid NestedGuidColumn { get; set; }
}
