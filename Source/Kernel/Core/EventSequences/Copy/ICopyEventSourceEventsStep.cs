// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.Copy;

/// <summary>
/// Defines the job step that performs the copy for <see cref="ICopyEventSourceEvents"/>.
/// </summary>
public interface ICopyEventSourceEventsStep : IJobStep<CopyEventSourceEventsRequest, object, CopyEventSourceEventsStepState>;
