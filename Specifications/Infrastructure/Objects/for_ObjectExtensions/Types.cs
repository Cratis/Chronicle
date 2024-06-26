// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Objects.for_ObjectExtensions;

#pragma warning disable SA1649 // File name should match first type name

public record TopLevel(FirstLevel? FirstLevel);
public record FirstLevel(IEnumerable<SecondLevel>? SecondLevel, string SomeProperty);
public record SecondLevel(string Identifier, ThirdLevel? ThirdLevel);
public record ThirdLevel(IEnumerable<ForthLevel>? ForthLevel);
public record ForthLevel(string Identifier, string? SomeProperty);
