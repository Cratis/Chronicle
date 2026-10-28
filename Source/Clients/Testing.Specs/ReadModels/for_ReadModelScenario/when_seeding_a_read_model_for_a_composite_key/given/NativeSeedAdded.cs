// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

[EventType]
public record NativeSeedAdded(Guid Identifier, int Number, long Large, bool Enabled, DateOnly Day, DateTimeOffset Occurred);
