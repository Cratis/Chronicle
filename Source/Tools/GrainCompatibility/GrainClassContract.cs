// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// A grain class as recorded in an OrleansContracts.txt.
/// </summary>
/// <param name="GrainType">The grain type Orleans identifies the class by, in grain ids, placement and the directory.</param>
/// <param name="ClrName">The CLR name of the class.</param>
/// <param name="Retired">Whether the class has been removed and is kept only as history.</param>
public record GrainClassContract(string GrainType, string ClrName, bool Retired);
