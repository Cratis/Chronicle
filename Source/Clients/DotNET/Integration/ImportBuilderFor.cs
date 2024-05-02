// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IImportBuilderFor{TModel, TExternalModel}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TExternalModel">The type of the external model.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ImportBuilderFor{TModel, TExternalModel}"/> class.
/// </remarks>
/// <param name="importContexts">Subject to build for.</param>
public class ImportBuilderFor<TModel, TExternalModel>(Subject<ImportContext<TModel, TExternalModel>> importContexts) : IImportBuilderFor<TModel, TExternalModel>
{
    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<ImportContext<TModel, TExternalModel>> observer) => importContexts.Subscribe(observer);
}
