// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reflection;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using AutoMapper;

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Represents an implementation of <see cref="IImportOperations{TModel, TExternalModel}"/>.
    /// </summary>
    /// <typeparam name="TModel">Type of model the operations are for.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model the operations are for.</typeparam>
    public class ImportOperations<TModel, TExternalModel> : IImportOperations<TModel, TExternalModel>
    {
        readonly Subject<ImportContext<TModel, TExternalModel>> _importContexts;
        readonly IEventLog _eventLog;

        /// <inheritdoc/>
        public IAdapterFor<TModel, TExternalModel> Adapter { get; }

        /// <inheritdoc/>
        public IAdapterProjectionFor<TModel> Projection { get; }

        /// <inheritdoc/>
        public IMapper Mapper { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportOperations{TModel, TExternalModel}"/> class.
        /// </summary>
        /// <param name="adapter">The <see cref="IAdapterFor{TModel, TExternalModel}"/>.</param>
        /// <param name="adapterProjection">The <see cref="IAdapterProjectionFor{TModel}"/> for the model.</param>
        /// <param name="mapper"><see cref="IMapper"/> to use for mapping beteween external model and model.</param>
        /// <param name="eventLog">The <see cref="IEventLog"/> to work with.</param>
        public ImportOperations(
            IAdapterFor<TModel, TExternalModel> adapter,
            IAdapterProjectionFor<TModel> adapterProjection,
            IMapper mapper,
            IEventLog eventLog)
        {
            Adapter = adapter;
            Projection = adapterProjection;
            Mapper = mapper;
            _importContexts = new();
            Adapter.DefineImport(new ImportBuilderFor<TModel, TExternalModel>(_importContexts));
            _eventLog = eventLog;
        }

        /// <inheritdoc/>
        public async Task Apply(TExternalModel instance)
        {
            var keyValue = Adapter.KeyResolver(instance)!;
            var eventSourceId = keyValue;
            eventSourceId ??= new(keyValue.ToString()!);
            var initial = Projection.GetById(eventSourceId!);
            var mappedInstance = Mapper.Map<TModel>(instance)!;
            var changeset = new Changeset<TModel, TModel>(mappedInstance, initial);

            var modelProperties = typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var comparer = new ObjectsComparer.Comparer<TModel>();
            if (!comparer.Compare(initial, mappedInstance, out var differences))
            {
                changeset.Add(new PropertiesChanged<TModel>(mappedInstance, differences.Select(_ => new PropertyDifference<TModel>(initial, mappedInstance, _))));
            }

            var context = new ImportContext<TModel, TExternalModel>(changeset, new EventsToAppend());
            _importContexts.OnNext(context);

            foreach (var @event in context.Events)
            {
                await _eventLog.Append(eventSourceId!, @event);
            }
        }

        /// <inheritdoc/>
        public async Task Apply(IEnumerable<TExternalModel> instances)
        {
            foreach (var instance in instances)
            {
                await Apply(instance);
            }
        }

        /// <inheritdoc/>
        public void Dispose() => _importContexts.Dispose();
    }
}
