// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_Reactors.when_registering_a_delegate;

[EventTypeGenerationFor<RuntimeGenerationEvent>(1)]
public record RuntimeGenerationEventV1(int Number);
