// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

public record ModuleNodeId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ModuleNodeId NotSet = new(Guid.Empty);
    public static implicit operator Guid(ModuleNodeId value) => value.Value;
    public static implicit operator ModuleNodeId(Guid value) => new(value);
    public static implicit operator EventSourceId(ModuleNodeId value) => new(value.Value.ToString());
    public static ModuleNodeId New() => new(Guid.NewGuid());
}
