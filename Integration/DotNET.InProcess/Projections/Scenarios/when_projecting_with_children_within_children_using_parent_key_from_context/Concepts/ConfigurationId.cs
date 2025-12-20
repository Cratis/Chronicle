// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Concepts;

public record ConfigurationId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ConfigurationId NotSet = new(Guid.Empty);
    public static implicit operator ConfigurationId(Guid value) => new(value);
    public static implicit operator EventSourceId(ConfigurationId id) => new(id.Value.ToString());
    public static ConfigurationId New() => new(Guid.NewGuid());
}
