// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the state of an <see cref="Import"/> grain.
/// </summary>
public class ImportGrainState
{
    /// <summary>
    /// Gets or sets the <see cref="ImportRecord"/>; null until the import is begun.
    /// </summary>
    public ImportRecord? Record { get; set; }
}
