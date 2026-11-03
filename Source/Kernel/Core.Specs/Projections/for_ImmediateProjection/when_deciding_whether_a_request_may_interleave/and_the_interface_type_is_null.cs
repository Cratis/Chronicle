// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization.Invocation;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_deciding_whether_a_request_may_interleave;

public class and_the_interface_type_is_null : Specification
{
    IInvokable _request;
    bool _result;

    void Establish()
    {
        _request = Substitute.For<IInvokable>();
        _request.GetInterfaceType().Returns((Type)null!);
    }

    void Because() => _result = ImmediateProjection.IsDefinitionChangeNotification(_request);

    [Fact] void should_not_allow_the_request() => _result.ShouldBeFalse();
}
