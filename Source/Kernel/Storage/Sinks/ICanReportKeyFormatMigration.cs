// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Optional sink capability for reporting that the stored composite keys use a legacy key format
/// that only a replay can rebuild in the current format.
/// </summary>
public interface ICanReportKeyFormatMigration
{
    /// <summary>
    /// Determines whether the read model's primary storage holds composite keys in a legacy key format.
    /// </summary>
    /// <returns>True when a replay is required to rebuild the keys in the current format, false if not.</returns>
    Task<bool> RequiresKeyFormatMigration();
}
