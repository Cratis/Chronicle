// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using AutoMapper;

namespace Aksio.Cratis.Integration.for_ImportOperations.given;

public class all_dependencies : Specification
{
    protected const string key = "851b7f30-51ee-47f1-9aa1-1c013e9d86a3";
    protected Mock<IAdapterFor<Model, ExternalModel>> adapter;
    protected Mock<IAdapterProjectionFor<Model>> projection;
    protected Mock<IMapper> mapper;
    protected Mock<IEventLog> event_log;
    protected Mock<IEventOutbox> event_outbox;

    void Establish()
    {
        adapter = new();
        projection = new();
        mapper = new();
        event_log = new();
        event_outbox = new();

        adapter.SetupGet(_ => _.KeyResolver).Returns((ExternalModel _) => new EventSourceId(key));
        adapter.Setup(_ => _.DefineImport(IsAny<IImportBuilderFor<Model, ExternalModel>>()))
            .Callback((IImportBuilderFor<Model, ExternalModel> builder) => builder.WithProperties(_ => _.SomeInteger, _ => _.SomeString).AppendEvent<Model, ExternalModel, SomeEvent>());
    }
}
