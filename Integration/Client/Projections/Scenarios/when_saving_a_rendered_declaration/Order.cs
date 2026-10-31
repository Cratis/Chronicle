// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_saving_a_rendered_declaration;

public record Order(string Name, bool IsActive, DateTimeOffset Occurred);
