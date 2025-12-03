// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.Projections.Concepts;

public record UserId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator Guid(UserId value) => value.Value;
    public static implicit operator UserId(string value) => new(Guid.Parse(value));
    public static implicit operator UserId(Guid value) => new(value);
}
