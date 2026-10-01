// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

[Command]
public record TheCommand
{
    public void Handle(ApplicationExtensionPointsRecorder recorder) => recorder.Handled = true;
}
