// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Storage.Sinks.for_ReplayContexts;

public class when_establishing : Specification
{
    static ModelName _model = "SomeModel";
    ReplayContexts _contexts;
    IReplayContextsStorage _storage;
    ReplayContext _context;

    void Establish()
    {
        _storage = Substitute.For<IReplayContextsStorage>();
        _contexts = new(_storage);
    }

    async Task Because() => _context = await _contexts.Establish(_model);

    [Fact] void should_return_context() => _context.ShouldNotBeNull();
    [Fact] void should_have_model_in_context() => _context.Model.ShouldEqual(_model);
    [Fact] void should_save_context() => _storage.Received(1).Save(_context);
}
