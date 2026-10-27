// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

/// <summary>
/// A sealed derived record has no externally extensible protected surface.
/// </summary>
public sealed record SampleSealedContract : SampleExtensibleContract;
