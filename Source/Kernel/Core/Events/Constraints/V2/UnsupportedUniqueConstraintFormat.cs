// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// The exception that is thrown when a unique constraint uses an unsupported scalar format or nested collection.
/// </summary>
/// <param name="format">The unsupported format.</param>
internal class UnsupportedUniqueConstraintFormat(string format)
    : Exception($"Unique-constraint encoding v2 does not support format '{format}'.");
