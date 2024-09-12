// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;

public record UserId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator Guid(UserId value) => value.Value;
    public static implicit operator UserId(Guid value) => new(value);
}