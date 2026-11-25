// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionsStorage;

/// <summary>
/// The JSON a populated projection definition is stored as. Older and newer kernels read each other's rows, so this must not change.
/// </summary>
static class StoredJson
{
    public const string Populated = """
        {
          "Owner": 1,
          "EventSequenceId": "event-log",
          "Identifier": "client-projection",
          "ReadModel": "client-model",
          "IsActive": true,
          "IsRewindable": true,
          "InitialModelState": {
            "count": 0,
            "details": {
              "names": [
                "a",
                "b"
              ]
            }
          },
          "From": {
            "added": {
              "Properties": {
                "name": "Name"
              },
              "Key": "Key",
              "ParentKey": "Parent"
            }
          },
          "Join": {
            "added": {
              "On": "name",
              "Properties": {
                "name": "Name"
              },
              "Key": "Key",
              "AutoMap": 1
            }
          },
          "Children": {
            "items": {
              "IdentifiedBy": "id",
              "All": {
                "Properties": {
                  "name": "Name"
                },
                "IncludeChildren": true,
                "AutoMap": 1,
                "Key": "Category"
              },
              "Ordering": null,
              "Owner": 3,
              "EventSequenceId": "[unspecified]",
              "Identifier": "[unspecified]",
              "ReadModel": "NotSet",
              "IsActive": true,
              "IsRewindable": false,
              "InitialModelState": {},
              "From": {
                "added": {
                  "Properties": {
                    "name": "Name"
                  },
                  "Key": "Key",
                  "ParentKey": "Parent"
                }
              },
              "Join": {
                "added": {
                  "On": "name",
                  "Properties": {
                    "name": "Name"
                  },
                  "Key": "Key",
                  "AutoMap": 1
                }
              },
              "Children": {},
              "FromDerivatives": [],
              "FromEvery": {
                "Properties": {
                  "name": "Name"
                },
                "IncludeChildren": true,
                "AutoMap": 1,
                "Key": "Category"
              },
              "RemovedWith": {
                "added": {
                  "Key": "Key",
                  "ParentKey": "Parent"
                }
              },
              "RemovedWithJoin": {
                "added": {
                  "Key": "Key"
                }
              },
              "FromEventProperty": {
                "Event": {
                  "Id": "added",
                  "Generation": 1,
                  "Tombstone": false
                },
                "PropertyExpression": "Items"
              },
              "LastUpdated": null,
              "Tags": null,
              "AutoMap": 1,
              "Nested": null,
              "SubscribesToAllEvents": false,
              "NoAutoMapProperties": null,
              "Scope": 0,
              "GenerationDelivery": 0,
              "Version": null,
              "HasLegacyStorageFields": false
            }
          },
          "FromDerivatives": [
            {
              "EventTypes": [
                {
                  "Id": "added",
                  "Generation": 1,
                  "Tombstone": false
                }
              ],
              "From": {
                "Properties": {
                  "name": "Name"
                },
                "Key": "Key",
                "ParentKey": "Parent"
              }
            }
          ],
          "FromEvery": {
            "Properties": {
              "name": "Name"
            },
            "IncludeChildren": true,
            "AutoMap": 1,
            "Key": "Category"
          },
          "RemovedWith": {
            "added": {
              "Key": "Key",
              "ParentKey": "Parent"
            }
          },
          "RemovedWithJoin": {
            "added": {
              "Key": "Key"
            }
          },
          "FromEventProperty": {
            "Event": {
              "Id": "added",
              "Generation": 1,
              "Tombstone": false
            },
            "PropertyExpression": "Items"
          },
          "LastUpdated": null,
          "Tags": [
            "one",
            "two"
          ],
          "AutoMap": 2,
          "Nested": {
            "details": {
              "IdentifiedBy": "id",
              "All": {
                "Properties": {
                  "name": "Name"
                },
                "IncludeChildren": true,
                "AutoMap": 1,
                "Key": "Category"
              },
              "Ordering": null,
              "Owner": 3,
              "EventSequenceId": "[unspecified]",
              "Identifier": "[unspecified]",
              "ReadModel": "NotSet",
              "IsActive": true,
              "IsRewindable": false,
              "InitialModelState": {},
              "From": {
                "added": {
                  "Properties": {
                    "name": "Name"
                  },
                  "Key": "Key",
                  "ParentKey": "Parent"
                }
              },
              "Join": {
                "added": {
                  "On": "name",
                  "Properties": {
                    "name": "Name"
                  },
                  "Key": "Key",
                  "AutoMap": 1
                }
              },
              "Children": {},
              "FromDerivatives": [],
              "FromEvery": {
                "Properties": {
                  "name": "Name"
                },
                "IncludeChildren": true,
                "AutoMap": 1,
                "Key": "Category"
              },
              "RemovedWith": {
                "added": {
                  "Key": "Key",
                  "ParentKey": "Parent"
                }
              },
              "RemovedWithJoin": {
                "added": {
                  "Key": "Key"
                }
              },
              "FromEventProperty": {
                "Event": {
                  "Id": "added",
                  "Generation": 1,
                  "Tombstone": false
                },
                "PropertyExpression": "Items"
              },
              "LastUpdated": null,
              "Tags": null,
              "AutoMap": 1,
              "Nested": null,
              "SubscribesToAllEvents": false,
              "NoAutoMapProperties": null,
              "Scope": 0,
              "GenerationDelivery": 0,
              "Version": null,
              "HasLegacyStorageFields": false
            }
          },
          "SubscribesToAllEvents": false,
          "NoAutoMapProperties": [
            "secret"
          ],
          "Scope": 1,
          "GenerationDelivery": 0,
          "Version": null,
          "HasLegacyStorageFields": false
        }
        """;
}
