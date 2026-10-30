// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

public class when_cleanup_also_fails : Specification
{
    readonly IOException _first = new("first failure");
    readonly IOException _cleanup = new("cleanup failed");
    readonly IOException _additionalCleanup = new("another cleanup failed");
    Exception? _result;
    bool _subsequentCleanup;

    async Task Because()
    {
        _result = await ChronicleFixture.Cleanup(() => ValueTask.FromException(_cleanup), _first);
        _result = await ChronicleFixture.Cleanup(() => ValueTask.FromException(_additionalCleanup), _result);
        _result = await ChronicleFixture.Cleanup(
            () =>
            {
                _subsequentCleanup = true;
                return ValueTask.CompletedTask;
            },
            _result);
    }

    [Fact] void should_preserve_the_first_failure() => _result.ShouldEqual(_first);
    [Fact] void should_attach_every_cleanup_failure() => ((AggregateException)_result!.Data["FixtureCleanupFailure"]!).Flatten().InnerExceptions.ShouldContainOnly(_cleanup, _additionalCleanup);
    [Fact] void should_continue_cleanup() => _subsequentCleanup.ShouldBeTrue();
}
