// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.given;

public record store_scoped_surface(string SurfaceId, IReadOnlySet<store_access> Access, IReadOnlySet<store_effect> Effects);
