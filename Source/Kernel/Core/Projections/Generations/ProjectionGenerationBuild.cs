// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Admits candidate builds through durable reservations and fixed grants, without dispatching work.
/// </summary>
/// <param name="enablement">The operational generation flag.</param>
/// <param name="keys">The current materialization identities.</param>
/// <param name="bindings">The authoritative generation catalog.</param>
/// <param name="builds">The durable reservation and execution storage.</param>
/// <param name="grants">The conditional writer admission authority.</param>
/// <param name="specifications">The immutable execution input restorer.</param>
public sealed class ProjectionGenerationBuild(
    IGenerationEnablement enablement,
    IMaterializationKeys keys,
    IMaterializationBindingStorage bindings,
    IGenerationBuildStorage builds,
    IGenerationWriterGrants grants,
    GenerationExecutionSpecifications specifications) : Grain, IProjectionGenerationBuild
{
    /// <inheritdoc/>
    public async Task<GenerationBuildAdmission> Start(GenerationBuildRequest request)
    {
        if (!enablement.Enabled) return new(GenerationBuildAdmissionResult.Disabled);
        var owner = this.GetPrimaryKeyString();
        var separator = owner.IndexOf('/');
        if (separator < 1 || owner[(separator + 1)..] != request.Key.Value) return new(GenerationBuildAdmissionResult.StaleKey);
        EventStoreName store = owner[..separator];
        try
        {
            if (await keys.For(store, request.Key.Namespace, request.Key.Projection) != request.Key) return new(GenerationBuildAdmissionResult.StaleKey);
        }
        catch (GenerationFloorNotEstablished)
        {
            return new(GenerationBuildAdmissionResult.Uninitialized);
        }

        MaterializationExecutionInputs inputs;
        try
        {
            inputs = specifications.Restore(request.Execution);
        }
        catch (InvalidMaterializationExecution)
        {
            return new(GenerationBuildAdmissionResult.Unsupported);
        }
        if (!MaterializationGenerationCapabilities.CanBuild(inputs.Projection.Scope, inputs.ReadModel.Sink.Type)) return new(GenerationBuildAdmissionResult.Unsupported);
        return await Reserve(store, request);
    }

    /// <summary>
    /// Reserves once per operation and maps storage refusals before writer admission.
    /// </summary>
    /// <param name="store">The owning event store.</param>
    /// <param name="request">The immutable operation request.</param>
    /// <returns>The reservation refusal or the candidate admission.</returns>
    async Task<GenerationBuildAdmission> Reserve(EventStoreName store, GenerationBuildRequest request)
    {
        GenerationReservation reservation;
        try
        {
            reservation = await builds.ReserveNext(store, request.Key, request.Execution, request.Operation, request.At);
        }
        catch (MaterializationBindingsNotSupported)
        {
            return new(GenerationBuildAdmissionResult.Unsupported);
        }
        catch (GenerationBuildConflict)
        {
            return new(GenerationBuildAdmissionResult.Conflict);
        }
        return reservation.Result switch
        {
            GenerationReservationResult.UnknownMaterialization or GenerationReservationResult.NotBound => new(GenerationBuildAdmissionResult.NotBound),
            GenerationReservationResult.CandidateInFlight => new(GenerationBuildAdmissionResult.CandidateInFlight, reservation.Record),
            GenerationReservationResult.ReservationCreated when reservation.Record is not null => await Admit(store, request, reservation.Record),
            _ => new(GenerationBuildAdmissionResult.Conflict)
        };
    }

    /// <summary>
    /// Reconciles the reserved generation with its current state before acquiring or recovering a fixed grant.
    /// </summary>
    /// <param name="store">The owning event store.</param>
    /// <param name="request">The immutable operation request.</param>
    /// <param name="reserved">The operation's original reservation record.</param>
    /// <returns>The current candidate admission.</returns>
    async Task<GenerationBuildAdmission> Admit(EventStoreName store, GenerationBuildRequest request, MaterializationGenerationRecord reserved)
    {
        var catalog = await bindings.Get(store, request.Key);
        if (catalog is null) return new(GenerationBuildAdmissionResult.NotBound);
        var record = catalog.Generations.SingleOrDefault(_ => _.Identity.Generation == reserved.Identity.Generation);
        if (record is null || record.Identity != reserved.Identity || record.Revision != request.Execution.Revision) return new(GenerationBuildAdmissionResult.Conflict);
        if (record.State == MaterializationGenerationState.Abandoned) return new(GenerationBuildAdmissionResult.Abandoned, record);
        var binding = new GenerationObserverBinding(request.Key, record.Identity, request.Execution.EventSequence, request.Execution.Revision);
        var grant = await grants.TryAcquire(store, binding, request.Operation, expectedEpoch: 0) ?? await grants.GetAdmitted(store, binding);
        return grant?.Attempt == request.Operation
            ? new(GenerationBuildAdmissionResult.Admitted, record, grant)
            : new(GenerationBuildAdmissionResult.GrantRefused, record);
    }
}
