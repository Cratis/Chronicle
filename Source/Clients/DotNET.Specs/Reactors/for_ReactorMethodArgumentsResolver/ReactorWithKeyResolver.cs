// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver;

public class ReactorWithKeyResolver : IReactor, ICanResolveReadModelKey
{
    public const string CustomKey = "custom-key";

    public ReadModelKey Resolve(object @event, EventContext context) => new(CustomKey);

    public Task WithReadModel(SomeEvent @event, SomeReadModel readModel) => Task.CompletedTask;
}
