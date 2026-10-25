// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>A new operation cannot adopt another operation's in-flight candidate.</summary>
public class when_another_operation_starts_while_a_candidate_is_in_flight : given.a_generation_build_owner
{
    /// <summary>The candidate reserved by the original operation.</summary>
    GenerationReservation _reservation;

    /// <summary>Reserves one operation and then supplies a different attempt identity.</summary>
    async Task Establish()
    {
        _reservation = await Reserve();
        _request = _request with { Operation = new(Guid.NewGuid()) };
    }

    /// <summary>Attempts admission for the second operation.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The owner reports the existing candidate.</summary>
    [Fact] void should_report_the_in_flight_candidate() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.CandidateInFlight);

    /// <summary>The refusal identifies the original candidate.</summary>
    [Fact] void should_return_the_existing_record() => _result.Record.ShouldEqual(_reservation.Record);

    /// <summary>The new operation receives no writer authority.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();

    /// <summary>No extra candidate is allocated.</summary>
    [Fact] async Task should_keep_one_candidate() => (await Binding()).Generations.Count.ShouldEqual(2);
}
