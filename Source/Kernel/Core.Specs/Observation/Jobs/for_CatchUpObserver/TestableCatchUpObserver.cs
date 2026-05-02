// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver;

/// <summary>
/// A testable subclass of <see cref="CatchUpObserver"/> that exposes the grain interface type
/// to satisfy the <c>IFoo → Foo</c> convention expected by the testing infrastructure.
/// </summary>
/// <param name="catchupServiceClient">The <see cref="IObserverServiceClient"/> for catch-up notifications.</param>
/// <param name="storage">The <see cref="IStorage"/> for accessing key indexes.</param>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
public class TestableCatchUpObserver(
    IObserverServiceClient catchupServiceClient,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<CatchUpObserver> logger)
    : CatchUpObserver(catchupServiceClient, storage, jsonSerializerOptions, logger), IGrainType
{
    /// <inheritdoc/>
    public Type GrainType => typeof(ICatchUpObserver);

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(CatchUpObserverRequest request) =>
        base.PrepareSteps(request);
}
