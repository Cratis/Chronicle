// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.Concepts;

public record RootId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly RootId NotSet = new(Guid.Empty);
    public static implicit operator Guid(RootId value) => value.Value;
    public static implicit operator RootId(Guid value) => new(value);
    public static implicit operator EventSourceId(RootId value) => new(value.Value.ToString());
    public static RootId New() => new(Guid.NewGuid());
}
