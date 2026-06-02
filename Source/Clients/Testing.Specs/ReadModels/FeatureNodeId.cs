// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

public record FeatureNodeId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly FeatureNodeId NotSet = new(Guid.Empty);
    public static implicit operator Guid(FeatureNodeId value) => value.Value;
    public static implicit operator FeatureNodeId(Guid value) => new(value);
    public static implicit operator EventSourceId(FeatureNodeId value) => new(value.Value.ToString());
    public static FeatureNodeId New() => new(Guid.NewGuid());
}
