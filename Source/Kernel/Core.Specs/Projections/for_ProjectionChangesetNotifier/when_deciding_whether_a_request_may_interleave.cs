// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.ReadModels;
using Orleans.Concurrency;
using Orleans.Serialization.Invocation;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier;

public class when_deciding_whether_a_request_may_interleave : Specification
{
    IInvokable _subscription;
    IInvokable[] _otherRequests;
    bool _subscriptionMayInterleave;
    bool[] _otherRequestsMayInterleave;
    CustomAttributeData _attribute;

    void Establish()
    {
        _subscription = CreateRequest(typeof(IProjectionChangesetNotifier), nameof(IProjectionChangesetNotifier.Subscribe));
        _otherRequests =
        [
            CreateRequest(typeof(IProjectionChangesetNotifier), nameof(IProjectionChangesetNotifier.Notify)),
            CreateRequest(typeof(IProjectionChangesetNotifier), nameof(IProjectionChangesetNotifier.NotifyMany)),
            CreateRequest(typeof(IProjectionChangesetNotifier), nameof(IProjectionChangesetNotifier.Unsubscribe)),
            CreateRequest(typeof(IProjectionChangesetNotifier), nameof(IProjectionChangesetNotifier.HasSubscribers)),
            CreateRequest(typeof(IReadModel), nameof(IProjectionChangesetNotifier.Subscribe)),
            CreateRequest(null!, nameof(IProjectionChangesetNotifier.Subscribe))
        ];
    }

    void Because()
    {
        _subscriptionMayInterleave = ProjectionChangesetNotifier.IsSubscription(_subscription);
        _otherRequestsMayInterleave = _otherRequests.Select(ProjectionChangesetNotifier.IsSubscription).ToArray();
        _attribute = typeof(ProjectionChangesetNotifier).GetCustomAttributesData().Single(_ => _.AttributeType == typeof(MayInterleaveAttribute));
    }

    [Fact] void should_allow_an_unflagged_subscription() => _subscriptionMayInterleave.ShouldBeTrue();
    [Fact] void should_not_mark_other_requests_as_subscriptions() => _otherRequestsMayInterleave.ShouldEqual([false, false, false, false, false, false]);
    [Fact] void should_reject_a_null_request() => ProjectionChangesetNotifier.IsSubscription(null!).ShouldBeFalse();
    [Fact] void should_configure_the_server_side_predicate() => _attribute.ShouldNotBeNull();
    [Fact] void should_use_the_subscription_predicate() => _attribute.ConstructorArguments[0].Value.ShouldEqual(nameof(ProjectionChangesetNotifier.IsSubscription));

    static IInvokable CreateRequest(Type interfaceType, string methodName)
    {
        var request = Substitute.For<IInvokable>();
        request.GetInterfaceType().Returns(interfaceType);
        request.GetMethodName().Returns(methodName);
        return request;
    }
}
