// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Releases.for_DormantObserverFloor;

public class when_releases_cannot_be_read : Specification
{
    bool _result;

    void Because() => _result = DormantObserverFloor.IsReached(null);

    [Fact] void should_not_be_reached() => _result.ShouldBeFalse();
}
