// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Identifies one ordinary or traced repetition within a sweep.</summary>
/// <param name="Id">The stable run identity.</param>
/// <param name="Repetition">The repetition number.</param>
/// <param name="Trace">Whether this is a separate trace repetition.</param>
internal sealed record RunIdentity(string Id, int Repetition, bool Trace);
