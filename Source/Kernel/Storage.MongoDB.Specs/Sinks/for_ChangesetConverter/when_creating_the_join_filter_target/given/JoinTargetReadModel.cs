// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_creating_the_join_filter_target.given;

/// <summary>
/// A read model carrying one column per stored BSON representation a join can be declared on, so the
/// conversion of a join key can be specified against every shape the schema can dictate.
/// </summary>
public class JoinTargetReadModel
{
    public string? Id { get; set; }

    public Guid GuidColumn { get; set; }

    public string? StringColumn { get; set; }

    public int IntColumn { get; set; }

    public JoinTargetNested? Nested { get; set; }

    public IEnumerable<JoinTargetChild>? Children { get; set; }
}
