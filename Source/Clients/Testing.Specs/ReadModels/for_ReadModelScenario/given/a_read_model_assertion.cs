// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

public class a_read_model_assertion : Specification
{
    protected ReadModelScenario<AssertionDocument> _scenario;
    protected AssertionDocument _document;
    protected EventSourceId _key;
    protected JoinCustomerId _customer;
    Defaults _defaults;

    void Establish()
    {
        _key = "document-key";
        _customer = new(Guid.Parse("3de78cad-f053-4421-a2ac-697e16d51090"));
        _document = new("Ada", 12m, [1, 2], new Dictionary<string, JoinCustomerId?> { ["owner"] = _customer, ["note"] = null });
        _defaults = new(new InMemoryClientArtifactsProvider { EventTypes = [typeof(Tallied)], Reducers = [typeof(AssertionDocumentReducer)] });
        _scenario = new(null, _defaults);
        _scenario.CollectReadModelFor(_key, _document);
    }

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }
}
