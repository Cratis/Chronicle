// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="Capture"/> between grains. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="Id">The <see cref="Id"/> member of the <see cref="Capture"/>.</param>
/// <param name="Name">The <see cref="Name"/> member of the <see cref="Capture"/>.</param>
/// <param name="Declaration">The <see cref="Declaration"/> member of the <see cref="Capture"/>.</param>
/// <param name="Status">The <see cref="Status"/> member of the <see cref="Capture"/>.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.CaptureSurrogate")]
public readonly record struct CaptureSurrogate(
    [property: Id(0)] CaptureId Id,
    [property: Id(1)] CaptureName Name,
    [property: Id(2)] CaptureDeclaration Declaration,
    [property: Id(3)] CaptureStatus Status);
