// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.Concepts;

public record ChildId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ChildId NotSet = new(Guid.Empty);
    public static implicit operator Guid(ChildId value) => value.Value;
    public static implicit operator ChildId(Guid value) => new(value);
    public static implicit operator EventSourceId(ChildId value) => new(value.Value.ToString());
    public static ChildId New() => new(Guid.NewGuid());
}
