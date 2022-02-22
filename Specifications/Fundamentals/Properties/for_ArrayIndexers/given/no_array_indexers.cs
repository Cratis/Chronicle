// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_ArrayIndexers.given;

public class no_array_indexers : Specification
{
    protected ArrayIndexers indexers;

    void Establish() => indexers = new ArrayIndexers(Array.Empty<ArrayIndexer>());
}
