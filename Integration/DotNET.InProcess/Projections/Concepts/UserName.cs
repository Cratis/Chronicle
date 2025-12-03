// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.Projections.Concepts;

public record UserName(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator string(UserName value) => value.Value;
    public static implicit operator UserName(string value) => new(value);
}
