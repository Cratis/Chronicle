// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_checking_key_conversion : Specification
{
    EventStoreForTesting _store;

    void Establish() => _store = new EventStoreForTesting();

    [Fact] void should_refuse_a_numeric_key() =>
        _store.GetDecisionReads().Admit<NumericSourceModel>().Reason.ShouldEqual(DecisionReadRefusalReason.KeyConversion);

    [Fact] void should_admit_a_plain_string_key() =>
        _store.GetDecisionReads().Admit<StringSourceModel>().IsAdmitted.ShouldBeTrue();

    [Passive]
    [FromEvent<ModuleCreated>]
    public record NumericSourceModel(int Id, string Name);

    [Passive]
    [FromEvent<ModuleCreated>]
    public record StringSourceModel(string Id, string Name);
}
