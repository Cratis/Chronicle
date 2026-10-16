// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_rereading_a_passive_projection.and_the_joined_subject_was_erased.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_rereading_a_passive_projection;

/// <summary>
/// Verifies that rereading a passive projection releases joined PII with the joined subject's current key.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_joined_subject_was_erased(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public string CaseId { get; } = Guid.NewGuid().ToString();
        public string AdvisorId { get; } = Guid.NewGuid().ToString();
        public PassiveCase? BeforeErasure { get; private set; }
        public PassiveCase? AfterErasure { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(PassiveCaseOpened), typeof(PassiveAdvisorNamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(PassiveCase)];

        async Task Establish()
        {
            await EventStore.EventLog.Append(AdvisorId, new PassiveAdvisorNamed("Grace Hopper"));
            await EventStore.EventLog.Append(CaseId, new PassiveCaseOpened(AdvisorId, "Ada Lovelace"));
            BeforeErasure = await EventStore.ReadModels.GetInstanceById<PassiveCase>(CaseId);
        }

        async Task Because()
        {
            await EventStore.PII.DeleteEncryptionKeyFor(AdvisorId);
            AfterErasure = await EventStore.ReadModels.GetInstanceById<PassiveCase>(CaseId);
        }
    }

    [Fact] void should_release_the_joined_name_before_erasure() => Context.BeforeErasure!.AdvisorName.ShouldEqual("Grace Hopper");
    [Fact] void should_erase_the_joined_name_on_the_second_read() => Context.AfterErasure!.AdvisorName.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_cases_own_pii() => Context.AfterErasure!.SubjectName.ShouldEqual("Ada Lovelace");
}

/// <summary>
/// A case was opened and assigned to an advisor.
/// </summary>
/// <param name="AdvisorId">The assigned advisor.</param>
/// <param name="SubjectName">The case subject's name.</param>
[EventType]
public record PassiveCaseOpened(string AdvisorId, [property: PII] string SubjectName);

/// <summary>
/// An advisor's name was recorded on the advisor's own event source.
/// </summary>
/// <param name="FullName">The advisor's name.</param>
[EventType]
public record PassiveAdvisorNamed([property: PII] string FullName);

/// <summary>
/// A passive case projection with PII from its own subject and a joined advisor.
/// </summary>
/// <param name="AdvisorId">The assigned advisor.</param>
/// <param name="SubjectName">The case subject's name.</param>
/// <param name="AdvisorName">The advisor's name.</param>
[Passive]
[FromEvent<PassiveCaseOpened>]
public record PassiveCase(
    string AdvisorId,
    [property: PII] string SubjectName,
    [property: PII]
    [Join<PassiveAdvisorNamed>(on: nameof(AdvisorId), eventPropertyName: nameof(PassiveAdvisorNamed.FullName))]
    string AdvisorName);

#pragma warning restore SA1402
