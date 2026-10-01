// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a replay target cannot be found and publication cannot be proven.
/// </summary>
/// <param name="target">The missing target.</param>
public class ReplayTargetMissing(string target) : Exception($"Replay target '{target}' is missing; publication must be reconciled before observation resumes");
