// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_StorePreparationGate.when_a_newer_revision_faults;

public class and_an_older_transport_outcome_is_published : Specification
{
    readonly Exception _preparationFailure = new("newer preparation failure");
    readonly Exception _transportFailure = new("older transport failure");
    StorePreparationGate _gate;
    Exception? _published;
    Exception? _atFault;
    Exception? _afterFailure;
    Exception? _afterSuccess;

    async Task Establish()
    {
        _gate = new(failure => _published = failure);
        await _gate.Discover(() => Task.CompletedTask);
    }

    async Task Because()
    {
        await Catch.Exception(() => _gate.Discover(() => Task.FromException(_preparationFailure)));
        _atFault = _published;
        _gate.PublishRegistrationOutcome(failure => _published = failure, _transportFailure);
        _afterFailure = _published;
        _gate.PublishRegistrationOutcome(failure => _published = failure);
        _afterSuccess = _published;
    }

    [Fact] void should_publish_the_preparation_failure_without_a_registration_attempt() => ReferenceEquals(_atFault, _preparationFailure).ShouldBeTrue();
    [Fact] void should_not_overwrite_it_with_an_older_transport_failure() => ReferenceEquals(_afterFailure, _preparationFailure).ShouldBeTrue();
    [Fact] void should_not_overwrite_it_with_an_older_success() => ReferenceEquals(_afterSuccess, _preparationFailure).ShouldBeTrue();
}
