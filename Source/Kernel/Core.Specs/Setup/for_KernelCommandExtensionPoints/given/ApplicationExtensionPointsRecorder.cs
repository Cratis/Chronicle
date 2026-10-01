// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

public class ApplicationExtensionPointsRecorder
{
    public int ScopesBegun { get; set; }
    public int FiltersRun { get; set; }
    public int KeysResolved { get; set; }
    public bool Handled { get; set; }
}
