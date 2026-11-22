// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels.for_ReadModelDefinitionsStorage;

/// <summary>
/// The document a read model definition is stored as.
/// </summary>
static class StoredDocument
{
    public const string Orders = """
        {
          "_id": "orders",
          "containerName": "orders-container",
          "displayName": "Orders",
          "owner": 1,
          "source": 1,
          "observerType": 2,
          "observerIdentifier": "orders-projection",
          "sink": {
            "configuration": {
              "$binary": {
                "base64": "AAAAAAAAAAAAAAAAAAAAAA==",
                "subType": "04"
              }
            },
            "type": "None",
            "eventSequence": null,
            "name": ""
          },
          "schemas": {
            "1": {
              "type": "object",
              "properties": {
                "id": {
                  "type": "string"
                }
              }
            },
            "2": {
              "type": "object",
              "properties": {
                "id": {
                  "type": "string"
                },
                "name": {
                  "type": "string"
                }
              }
            }
          },
          "indexes": [
            "name"
          ],
          "indexDefinitions": [
            {
              "properties": [
                {
                  "path": "name",
                  "descending": false
                }
              ]
            }
          ]
        }
        """;
}
