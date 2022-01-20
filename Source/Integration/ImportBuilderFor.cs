// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Represents an implementation of <see cref="IImportBuilderFor{TModel, TExternalModel}"/>.
    /// </summary>
    /// <typeparam name="TModel">Model to build for.</typeparam>
    /// <typeparam name="TExternalModel">The type of the external model.</typeparam>
    public class ImportBuilderFor<TModel, TExternalModel> : IImportBuilderFor<TModel, TExternalModel>
    {
        readonly Subject<ImportContext<TModel, TExternalModel>> _importContexts;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportBuilderFor{TModel, TExternalModel}"/> class.
        /// </summary>
        /// <param name="importContexts">Subject to build for.</param>
        public ImportBuilderFor(Subject<ImportContext<TModel, TExternalModel>> importContexts)
        {
            _importContexts = importContexts;
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<ImportContext<TModel, TExternalModel>> observer) => _importContexts.Subscribe(observer);
    }
}
