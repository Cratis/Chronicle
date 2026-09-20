// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Jobs.for_JobStep.given;

/// <summary>
/// Defines the test job step grain.
/// </summary>
public interface ISomeJobStepGrain : IJobStep<SomeRequest, object, JobStepState>, IJobObserver;
