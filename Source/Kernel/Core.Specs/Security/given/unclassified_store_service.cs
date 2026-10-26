// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.given;

// Deliberately not included in either the hand-written contracts or the accepted operation inventory.
public interface unclassified_store_service
{
    Task Execute(unclassified_store_request request);

    Task ExecuteNested(nested_store_request request);

    Task ExecuteConcept(concept_store_request request);

    Task ExecuteGlobal(string value);
}

public record unclassified_store_request(string EventStore);

public record nested_store_request(IReadOnlyList<unclassified_store_request> Requests);

public record concept_store_request(EventStoreName EventStore);
