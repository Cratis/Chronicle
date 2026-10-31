// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Records the read-model argument received by a handler.
/// </summary>
public interface IRecordScenarioReadModel
{
    /// <summary>
    /// Records a resolved read model, including intentional absence.
    /// </summary>
    /// <param name="readModel">The handler argument.</param>
    void Record(object? readModel);
}
