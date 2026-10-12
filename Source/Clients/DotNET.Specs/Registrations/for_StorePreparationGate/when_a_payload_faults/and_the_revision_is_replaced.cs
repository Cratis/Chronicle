// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_StorePreparationGate.when_a_payload_faults;

public class and_the_revision_is_replaced : Specification
{
    readonly Exception _payloadFailure = new("payload failure");
    StorePreparationGate _gate;
    Exception? _published;
    Exception? _atFault;
    Exception? _afterRecovery;

    void Establish() => _gate = new(failure => _published = failure);

    async Task Because()
    {
        await _gate.Register(() => Task.CompletedTask, () =>
        {
            _gate.RecordPayloadFailure(_payloadFailure);
            _gate.RecordPayloadFailure(new Exception("later failure"));
            _atFault = _published;
            return Task.CompletedTask;
        });
        await _gate.Discover(() => Task.CompletedTask);
        await _gate.Register(() => Task.CompletedTask, () =>
        {
            _gate.PublishRegistrationOutcome(failure => _published = failure);
            return Task.CompletedTask;
        });
        _afterRecovery = _published;
    }

    [Fact] void should_publish_the_first_exact_payload_failure_at_the_point_it_faults() => ReferenceEquals(_atFault, _payloadFailure).ShouldBeTrue();
    [Fact] void should_allow_success_from_a_new_prepared_revision() => _afterRecovery.ShouldBeNull();
}
