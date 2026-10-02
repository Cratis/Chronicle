// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.given;

public static class CollisionAssembly
{
    static readonly ConcurrentDictionary<string, byte[]> _images = new(StringComparer.Ordinal);

    public static byte[] Compile(string name) => _images.GetOrAdd(name, name =>
    {
        // Share dependency images and runtime assemblies: duplicate loads of a referenced assembly interfere
        // with the process-wide type discovery used by other specs in this test host.
        const string Source = """
            namespace Collision;
            public static class Helper { public static int Apply(int value) => value; }
            public static class Helper<T> { public static int Apply(int value) => value; }
            """;
        return CompiledReducer.CompileAssembly(name, Source);
    });
}
