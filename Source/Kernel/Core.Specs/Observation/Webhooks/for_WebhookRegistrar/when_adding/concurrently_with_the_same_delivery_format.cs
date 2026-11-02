// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class concurrently_with_the_same_delivery_format : given.a_lagging_legacy_registration
{
    void Establish()
    {
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        _log.TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>()).Returns(async _ =>
        {
            if (Interlocked.Increment(ref reads) == 2) bothRead.SetResult();
            await bothRead.Task;
            return Cratis.Monads.Option<AppendedEvent>.None();
        });
    }

    /// <summary>Source equivalence: base 130c50244a WebhookRegistrar.cs:85-109 and :421-428 let both concurrent New writes append.</summary>
    /// <returns>Awaitable task.</returns>
    async Task Because() => _error = await Catch.Exception(() => Task.WhenAll(_registrar.Add("store", [_definition]), _registrar.Add("store", [_definition])));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_both_registrations() => _appended.OfType<WebhookAdded>().Count().ShouldEqual(2);
    [Fact] void should_append_both_authorizations() => _appended.OfType<BearerTokenAuthorizationSetForWebhook>().Count().ShouldEqual(2);
    [Fact] void should_not_fence_either_write() => ScopedAppends.ShouldBeEmpty();
    [Fact] void should_not_retry() => LifecycleReads.ShouldEqual(2);
}
