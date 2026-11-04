// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion.given;

[ReadModel]
[AllowAnonymous]
public record ApplicationQuery(int Value)
{
    public static ApplicationQuery Get() => new(42);
}
