// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents the content root of the project.
/// </summary>
/// <param name="Value">The string path.</param>
public record ContentRoot(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator string(ContentRoot value) => value.Value;
    public static implicit operator ContentRoot(string value) => new(value);
}
