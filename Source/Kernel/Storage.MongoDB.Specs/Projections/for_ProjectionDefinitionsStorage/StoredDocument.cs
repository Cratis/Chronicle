// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionsStorage;

/// <summary>
/// The document a populated projection definition is stored as.
/// </summary>
static class StoredDocument
{
    public const string Populated = """
        {
          "_id": "client-projection",
          "owner": 1,
          "readModel": {
            "identifier": "client-model",
            "generation": 1
          },
          "definitions": {
            "1": {
              "owner": 1,
              "eventSequenceId": "event-log",
              "identifier": "client-projection",
              "readModel": "client-model",
              "isActive": true,
              "isRewindable": true,
              "initialModelState": {
                "count": 0,
                "details": {
                  "names": [
                    "a",
                    "b"
                  ]
                }
              },
              "from": {
                "added+1": {
                  "properties": {
                    "name": "Name"
                  },
                  "key": "Key",
                  "parentKey": "Parent"
                }
              },
              "join": {
                "added+1": {
                  "on": {
                    "path": "name"
                  },
                  "properties": {
                    "name": "Name"
                  },
                  "key": "Key",
                  "autoMap": 1
                }
              },
              "children": {
                "items": {
                  "from": {
                    "added+1": {
                      "properties": {
                        "name": "Name"
                      },
                      "key": "Key",
                      "parentKey": "Parent"
                    }
                  },
                  "join": {
                    "added+1": {
                      "on": {
                        "path": "name"
                      },
                      "properties": {
                        "name": "Name"
                      },
                      "key": "Key",
                      "autoMap": 1
                    }
                  },
                  "children": {},
                  "fromEvery": {
                    "properties": {
                      "name": "Name"
                    },
                    "includeChildren": true,
                    "key": "Category",
                    "autoMap": 1
                  },
                  "removedWith": {
                    "added+1": {
                      "key": "Key",
                      "parentKey": "Parent"
                    }
                  },
                  "removedWithJoin": {
                    "added+1": {
                      "key": "Key"
                    }
                  },
                  "fromEventProperty": {
                    "event": {
                      "_id": "added",
                      "generation": 1
                    },
                    "propertyExpression": "Items"
                  },
                  "identifiedBy": "id",
                  "autoMap": 1,
                  "nested": {},
                  "noAutoMapProperties": []
                }
              },
              "fromDerivatives": [
                {
                  "eventTypes": [
                    {
                      "_id": "added",
                      "generation": 1
                    }
                  ],
                  "from": {
                    "properties": {
                      "name": "Name"
                    },
                    "key": "Key",
                    "parentKey": "Parent"
                  }
                }
              ],
              "fromEvery": {
                "properties": {
                  "name": "Name"
                },
                "includeChildren": true,
                "key": "Category",
                "autoMap": 1
              },
              "removedWith": {
                "added+1": {
                  "key": "Key",
                  "parentKey": "Parent"
                }
              },
              "removedWithJoin": {
                "added+1": {
                  "key": "Key"
                }
              },
              "fromEventProperty": {
                "event": {
                  "_id": "added",
                  "generation": 1
                },
                "propertyExpression": "Items"
              },
              "lastUpdated": null,
              "tags": [
                "one",
                "two"
              ],
              "autoMap": 2,
              "nested": {
                "details": {
                  "from": {
                    "added+1": {
                      "properties": {
                        "name": "Name"
                      },
                      "key": "Key",
                      "parentKey": "Parent"
                    }
                  },
                  "join": {
                    "added+1": {
                      "on": {
                        "path": "name"
                      },
                      "properties": {
                        "name": "Name"
                      },
                      "key": "Key",
                      "autoMap": 1
                    }
                  },
                  "children": {},
                  "fromEvery": {
                    "properties": {
                      "name": "Name"
                    },
                    "includeChildren": true,
                    "key": "Category",
                    "autoMap": 1
                  },
                  "removedWith": {
                    "added+1": {
                      "key": "Key",
                      "parentKey": "Parent"
                    }
                  },
                  "removedWithJoin": {
                    "added+1": {
                      "key": "Key"
                    }
                  },
                  "fromEventProperty": {
                    "event": {
                      "_id": "added",
                      "generation": 1
                    },
                    "propertyExpression": "Items"
                  },
                  "identifiedBy": "id",
                  "autoMap": 1,
                  "nested": {},
                  "noAutoMapProperties": []
                }
              },
              "subscribesToAllEvents": false,
              "noAutoMapProperties": [
                "secret"
              ],
              "generationDelivery": 0,
              "version": null,
              "scope": 1,
              "storageFormatVersion": 1,
              "graph": null
            }
          }
        }
        """;
}
