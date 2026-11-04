// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization.Invocation;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_deciding_whether_a_request_may_interleave;

public class and_the_request_is_for_another_interface : Specification
{
    bool[] _results;
    IInvokable[] _requests;

    void Establish()
    {
        (Type Interface, string Method)[] calls =
        [
            (typeof(IImmediateProjection), nameof(IImmediateProjection.GetModelInstance)),
            (typeof(IProjection), nameof(INotifyProjectionDefinitionsChanged.OnProjectionDefinitionsChanged)),
            (typeof(given.IDerivedNotification), nameof(INotifyProjectionDefinitionsChanged.OnProjectionDefinitionsChanged)),
            (typeof(IImmediateProjection), nameof(IImmediateProjection.Dehydrate)),
            (typeof(IProjection), nameof(IProjection.GetDefinition))
        ];
        _requests = calls.Select(call =>
        {
            var request = Substitute.For<IInvokable>();
            request.GetInterfaceType().Returns(call.Interface);
            request.GetMethodName().Returns(call.Method);
            return request;
        }).ToArray();
    }

    void Because() => _results = _requests.Select(ImmediateProjection.IsDefinitionChangeNotification).ToArray();

    [Fact] void should_not_allow_an_immediate_projection_request() => _results[0].ShouldBeFalse();
    [Fact] void should_not_allow_a_projection_request_with_the_same_method_name() => _results[1].ShouldBeFalse();
    [Fact] void should_not_allow_a_derived_notification_interface() => _results[2].ShouldBeFalse();
    [Fact] void should_not_allow_dehydration() => _results[3].ShouldBeFalse();
    [Fact] void should_not_allow_an_ordinary_projection_request() => _results[4].ShouldBeFalse();
}
