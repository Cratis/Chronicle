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
    protected IAdapterFor<Model, ExternalModel> _adapter;
    protected IAdapterProjectionFor<Model> _projection;
    protected IMapper _mapper;
    protected IEventLog _eventLog;
    protected IObjectComparer _objectsComparer;
    protected ICausationManager causation_manager;
    protected AdapterId _adapterId;
    protected IDictionary<string, string> _causationProperties;

    void Establish()
    {
        _adapter = Substitute.For<IAdapterFor<Model, ExternalModel>>();
        _projection = Substitute.For<IAdapterProjectionFor<Model>>();
        _mapper = Substitute.For<IMapper>();
        _eventLog = Substitute.For<IEventLog>();
        _objectsComparer = Substitute.For<IObjectComparer>();
        causation_manager = Substitute.For<ICausationManager>();

        _adapterId = Guid.NewGuid().ToString();
        _adapter.Identifier.Returns(_adapterId);
        _adapter.KeyResolver.Returns((ExternalModel _) => new EventSourceId(key));
        _adapter.When(_ => _.DefineImport(Arg.Any<IImportBuilderFor<Model, ExternalModel>>()))
            .Do(callInfo =>
            {
                var builder = callInfo.Arg<IImportBuilderFor<Model, ExternalModel>>();
                builder.WithProperties(_ => _.SomeInteger, _ => _.SomeString).AppendEvent<Model, ExternalModel, TEvent>();
            });

        causation_manager
            .When(_ => _.Add(ImportOperations<string, string>.CausationType, Arg.Any<IDictionary<string, string>>()))
            .Do(callInfo => _causationProperties = callInfo.Arg<IDictionary<string, string>>());
    }
}
