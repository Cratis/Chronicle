// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json;

/// <summary>
/// The scalars a string in JSON can be read directly into.
/// </summary>
enum StringScalar
{
    /// <summary>The string is not read directly into a scalar.</summary>
    None = 0,

    /// <summary>The string is a <see cref="Guid"/>.</summary>
    Guid = 1,

    /// <summary>The string is a <see cref="DateTime"/>.</summary>
    DateTime = 2
}
