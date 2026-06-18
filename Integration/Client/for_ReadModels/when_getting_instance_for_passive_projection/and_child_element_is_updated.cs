// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection.and_child_element_is_updated.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_instance_for_passive_projection;

[Collection(ChronicleCollection.Name)]
public class and_child_element_is_updated(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid ContractId;
        public Guid CandidateId;
        public PassiveContract Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(PassiveContractCreated),
            typeof(PassiveCandidateSubmitted),
            typeof(PassiveCandidateSigned)
        ];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(PassiveContract)];

        async Task Because()
        {
            ContractId = Guid.Parse("8b95cb59-3a99-4bda-a11a-cd0000000001");
            CandidateId = Guid.Parse("8b95cb59-3a99-4bda-a11a-cd0000000002");

            await EventStore.EventLog.Append(ContractId.ToString(), new PassiveContractCreated("Master Services Agreement"));
            await EventStore.EventLog.Append(ContractId.ToString(), new PassiveCandidateSubmitted(CandidateId, "Ada Lovelace"));
            await EventStore.EventLog.Append(ContractId.ToString(), new PassiveCandidateSigned(CandidateId, "Signed in the portal"));

            Result = await EventStore.ReadModels.GetInstanceById<PassiveContract>(ContractId.ToString());
        }
    }

    [Fact] void should_return_the_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_one_candidate() => Context.Result.Candidates.Count.ShouldEqual(1);
    [Fact] void should_keep_candidate_identifier() => Candidate.CandidateId.ShouldEqual(Context.CandidateId);
    [Fact] void should_set_candidate_as_signed() => Candidate.IsSigned.ShouldBeTrue();
    [Fact] void should_set_candidate_signing_notes() => Candidate.SigningNotes.ShouldEqual("Signed in the portal");

    PassiveCandidate Candidate => Context.Result.Candidates.Single();
}

[EventType]
public record PassiveContractCreated(string Name);

[EventType]
public record PassiveCandidateSubmitted(Guid CandidateId, string Name);

[EventType]
public record PassiveCandidateSigned(Guid CandidateId, string Notes);

[Passive]
[FromEvent<PassiveContractCreated>]
public record PassiveContract(
    Guid Id,
    string Name,
    [ChildrenFrom<PassiveCandidateSubmitted>(key: nameof(PassiveCandidateSubmitted.CandidateId), identifiedBy: nameof(PassiveCandidate.CandidateId))]
    IReadOnlyList<PassiveCandidate> Candidates);

[FromEvent<PassiveCandidateSubmitted>]
[FromEvent<PassiveCandidateSigned>(key: nameof(PassiveCandidateSigned.CandidateId))]
public record PassiveCandidate(
    [Key] Guid CandidateId,
    [SetValue<PassiveCandidateSigned>(true)] bool IsSigned,
    [SetFrom<PassiveCandidateSigned>(nameof(PassiveCandidateSigned.Notes))] string SigningNotes);

#pragma warning restore SA1402
