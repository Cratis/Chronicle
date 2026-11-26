// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Assertions.for_ExpectedValueComparer.given;

public class IndexedShape
{
    public static string Static => throw new InvalidOperationException("Static properties are not part of a shape.");
    public string Name => "Ada";
    public string this[int index] => throw new InvalidOperationException("Indexers are not part of a shape.");
}
