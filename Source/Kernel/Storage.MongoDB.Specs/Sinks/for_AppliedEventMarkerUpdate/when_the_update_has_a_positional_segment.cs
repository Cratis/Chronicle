// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_the_update_has_a_positional_segment : given.a_marked_update
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => AppliedEventMarkerUpdate.Build([Own(Updates.Set("children.$[child].name", "x"))]));

    [Fact] void should_not_be_supported() => _result.ShouldBeOfExactType<AppliedEventMarkerUpdateNotSupported>();
}
