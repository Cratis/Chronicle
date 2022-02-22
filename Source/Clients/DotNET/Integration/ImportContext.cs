// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents the context when translating input for an <see cref="AdapterFor{TModel, TExternalModel}"/>.
/// </summary>
/// <param name="Changeset">The <see cref="Changeset{TModel, TModel}"/> in the context..</param>
/// <param name="Events">Any <see cref="EventsToAppend"/>.</param>
/// <typeparam name="TModel">Type of model the translation is for.</typeparam>
/// <typeparam name="TExternalModel">Type of the external model to do translation from.</typeparam>
public record ImportContext<TModel, TExternalModel>(Changeset<TModel, TModel> Changeset, EventsToAppend Events);
