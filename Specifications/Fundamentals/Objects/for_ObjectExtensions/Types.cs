// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Objects.for_ObjectExtensions;

public record TopLevel(FirstLevel? FirstLevel);
public record FirstLevel(IEnumerable<SecondLevel>? SecondLevel, string SomeProperty);
public record SecondLevel(string Identifier, ThirdLevel? ThirdLevel);
public record ThirdLevel(IEnumerable<ForthLevel>? ForthLevel);
public record ForthLevel(string Identifier, string? SomeProperty);
