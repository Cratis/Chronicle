// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes.for_ObjectComparer.given;

public class an_object_comparer : Specification
{
    protected ObjectComparer comparer;

    void Establish() => comparer = new();
}
