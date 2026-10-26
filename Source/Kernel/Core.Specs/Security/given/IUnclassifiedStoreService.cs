// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.given;

/// <summary>
/// Deliberately not included in either the hand-written contracts or the accepted operation inventory.
/// </summary>
public interface IUnclassifiedStoreService
{
    Task Execute(unclassified_store_request request);

    Task ExecuteNested(nested_store_request request);

    Task ExecuteConcept(concept_store_request request);

    Task ExecuteGlobal(string value);
}
