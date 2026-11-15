// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the state of the <see cref="ImportEventsStep"/>.
/// </summary>
/// <remarks>
/// The commit's checkpoint, the last committed chunk and the sequence numbers it was given, is kept on the import
/// itself so it can be observed; the step state only names the import.
/// </remarks>
public class ImportEventsStepState : JobStepState
{
    /// <summary>
    /// Gets or sets the import being committed.
    /// </summary>
    public ImportId ImportId { get; set; } = ImportId.NotSet;
}
