// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_reading_persisted_claims_for_a_batch;

public class and_the_storage_answers_many_claims_at_once : Specification
{
    const string EventTypeId = "SomeEvent";
    const string PropertyName = "SomeProperty";
    static readonly EventSourceId _holder = "holder";
    static readonly EventSourceId _newcomer = "newcomer";
    static readonly EventSourceId _other = "other";

    IUniqueConstraintsBatchStorage _storage;
    UniqueConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationContext _takenByAnother;
    ConstraintValidationContext _free;
    IReadOnlyList<UniqueConstraintClaim> _askedFor;
    ConstraintValidationResult _takenByAnotherResult;
    ConstraintValidationResult _freeResult;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsBatchStorage>();
        _validator = new UniqueConstraintValidator(new UniqueConstraintDefinition("some-constraint", [new(EventTypeId, [PropertyName])]), _storage);
        _batchClaims = new();
        _takenByAnother = ContextFor(_newcomer, "taken");
        _free = ContextFor(_other, "free");

        var taken = ValueOf("taken");
        _storage.IsAllowedMany(Arg.Any<IReadOnlyList<UniqueConstraintClaim>>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _askedFor = call.ArgAt<IReadOnlyList<UniqueConstraintClaim>>(0);
                IReadOnlyList<(bool IsAllowed, EventSequenceNumber SequenceNumber)> answers = [.. _askedFor.Select(claim => claim.Value == taken && claim.EventSourceId != _holder ? (false, (EventSequenceNumber)42UL) : (true, EventSequenceNumber.Unavailable))];
                return Task.FromResult(answers);
            });
    }

    async Task Because()
    {
        await _validator.ReadPersistedClaims([_takenByAnother, _free], _batchClaims);
        _takenByAnotherResult = await _validator.Validate(_takenByAnother);
        _freeResult = await _validator.Validate(_free);
    }

    [Fact] void should_ask_for_every_claim_in_one_read() => _askedFor.Count.ShouldEqual(2);
    [Fact] void should_refuse_the_value_held_by_another_event_source() => _takenByAnotherResult.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_sequence_number_holding_the_value() => _takenByAnotherResult.Violations[0].SequenceNumber.ShouldEqual(42U);
    [Fact] void should_allow_the_free_value() => _freeResult.IsValid.ShouldBeTrue();
    [Fact] async Task should_not_read_the_index_one_claim_at_a_time() => await _storage.DidNotReceive().IsAllowed(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

    static UniqueConstraintValue ValueOf(string value) => new List<UniqueConstraintPropertyAndValue> { new(PropertyName, value) }.GetValue();

    ConstraintValidationContext ContextFor(EventSourceId eventSourceId, string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)[PropertyName] = value;
        return new([_validator], eventSourceId, EventTypeId, content, batchClaims: _batchClaims);
    }
}
