// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion.given;

[Command]
[Authorize(Policy = "Application.Required")]
public record ApplicationCommand
{
    public int Handle() => 42;
}
