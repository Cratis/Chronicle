// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Links an inventory row to exactly one successor obligation of its revision.
/// </summary>
/// <param name="IsRestartPlan">True for a restart plan, false for a continuation.</param>
/// <param name="Id">The origin row's stable identity.</param>
public record ObserverJobOrigin(bool IsRestartPlan, string Id);
