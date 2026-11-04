// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_deciding_whether_a_request_may_interleave;

public class and_the_request_is_null : Specification
{
    bool _result;

    void Because() => _result = ImmediateProjection.IsDefinitionChangeNotification(null!);

    [Fact] void should_not_allow_the_request() => _result.ShouldBeFalse();
}
