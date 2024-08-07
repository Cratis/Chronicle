// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_ImportOperations.given;

public class all_dependencies_for<TEvent> : Specification
{
    protected const string key = "851b7f30-51ee-47f1-9aa1-1c013e9d86a3";
    protected Mock<IAdapterFor<Model, ExternalModel>> adapter;
    protected Mock<IAdapterProjectionFor<Model>> projection;
    protected Mock<IMapper> mapper;
    protected Mock<IEventLog> event_log;
    protected Mock<IObjectComparer> objects_comparer;
    protected Mock<ICausationManager> causation_manager;

    protected AdapterId adapter_id;
    protected IDictionary<string, string> causation_properties;

    void Establish()
    {
        adapter = new();
        projection = new();
        mapper = new();
        event_log = new();
        objects_comparer = new();
        causation_manager = new();

        adapter_id = Guid.NewGuid().ToString();
        adapter.SetupGet(_ => _.Identifier).Returns(adapter_id);
        adapter.SetupGet(_ => _.KeyResolver).Returns((ExternalModel _) => new EventSourceId(key));
        adapter.Setup(_ => _.DefineImport(IsAny<IImportBuilderFor<Model, ExternalModel>>()))
            .Callback((IImportBuilderFor<Model, ExternalModel> builder)
                => builder.WithProperties(_ => _.SomeInteger, _ => _.SomeString).AppendEvent<Model, ExternalModel, TEvent>());

        causation_manager
            .Setup(_ => _.Add(ImportOperations<string, string>.CausationType, IsAny<IDictionary<string, string>>()))
            .Callback((CausationType type, IDictionary<string, string> properties) => causation_properties = properties);
    }
}
