// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

/// <summary>
/// What reading an occurrence through a sink returned.
/// </summary>
/// <param name="Total">The total count of the list query.</param>
/// <param name="Listed">The number of instances the list query returned.</param>
/// <param name="Observed">The number of instances the first emission of the live query held.</param>
public record OccurrenceRead(long Total, int Listed, int Observed);
