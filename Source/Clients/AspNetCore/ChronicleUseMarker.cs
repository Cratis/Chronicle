// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore;

/// <summary>
/// Records whether <c language="csharp">UseCratisChronicle</c> has been called, so startup can say so when it was forgotten.
/// </summary>
internal sealed class ChronicleUseMarker
{
    volatile bool _called;

    /// <summary>
    /// Gets a value indicating whether <c language="csharp">UseCratisChronicle</c> has been called.
    /// </summary>
    public bool Called => _called;

    /// <summary>
    /// Records that <c language="csharp">UseCratisChronicle</c> has been called.
    /// </summary>
    public void MarkCalled() => _called = true;
}
