---
title: Cratis.Chronicle.Server v1.0
language_tabs: []
toc_footers: []
includes: []
search: true
highlight_theme: darkula
headingLevel: 2

---

<!-- Generator: Widdershins v4.0.1 -->

<h1 id="cratis-kernel-server">Cratis.Chronicle.Server v1.0</h1>

> Scroll down for example requests and responses.

<h1 id="cratis-kernel-server-clientobservers">ClientObservers</h1>

## post__.cratis_observers_{observerId}

`POST /.cratis/observers/{observerId}`

*Action that is called for events to be handled.*

> Body parameter

```json
{
  "metadata": {
    "sequenceNumber": {
      "value": 0
    },
    "type": {
      "id": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "generation": {
        "value": 0
      },
      "isPublic": true
    }
  },
  "context": {
    "eventSourceId": {
      "value": "string"
    },
    "sequenceNumber": {
      "value": 0
    },
    "occurred": "2019-08-24T14:15:22Z",
    "validFrom": "2019-08-24T14:15:22Z",
    "tenantId": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "correlationId": {
      "value": "string"
    },
    "causationId": {
      "value": "string"
    },
    "causedBy": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "observationState": 0
  },
  "content": {
    "property1": null,
    "property2": null
  }
}
```

<h3 id="post__.cratis_observers_{observerid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|observerId|path|[ObserverId](#schemaobserverid)|true|The Cratis.Observation.ObserverId of the observer it is for.|
|body|body|[AppendedEvent](#schemaappendedevent)|false|The Cratis.Events.AppendedEvent.|

<h3 id="post__.cratis_observers_{observerid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-connectedclients">ConnectedClients</h1>

## post__api_clients_{microserviceId}_ping_{connectionId}

`POST /api/clients/{microserviceId}/ping/{connectionId}`

*A ping endpoint for clients to see if Kernel is available.*

<h3 id="post__api_clients_{microserviceid}_ping_{connectionid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|The Cratis.Execution.MicroserviceId that is connecting.|
|connectionId|path|[ConnectionId](#schemaconnectionid)|true|The unique identifier of the connection that is pinging.|

<h3 id="post__api_clients_{microserviceid}_ping_{connectionid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## post__api_clients_{microserviceId}_connect_{connectionId}

`POST /api/clients/{microserviceId}/connect/{connectionId}`

*Accepts client connections over Web Sockets.*

> Body parameter

```json
{
  "clientVersion": "string",
  "advertisedUri": "string"
}
```

<h3 id="post__api_clients_{microserviceid}_connect_{connectionid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|The Cratis.Execution.MicroserviceId that is connecting.|
|connectionId|path|[ConnectionId](#schemaconnectionid)|true|The unique identifier of the connection.|
|body|body|[ClientInformation](#schemaclientinformation)|false|Cratis.Clients.ClientInformation to connect with.|

<h3 id="post__api_clients_{microserviceid}_connect_{connectionid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-eventsequence">EventSequence</h1>

## post__api_events_store_{microserviceId}_{tenantId}_sequence_{eventSequenceId}

`POST /api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}`

*Appends an event to the event log.*

> Body parameter

```json
{
  "eventSourceId": {
    "value": "string"
  },
  "eventType": {
    "id": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "generation": {
      "value": 0
    },
    "isPublic": true
  },
  "content": {
    "property1": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    },
    "property2": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    }
  }
}
```

<h3 id="post__api_events_store_{microserviceid}_{tenantid}_sequence_{eventsequenceid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|The microservice to append for.|
|eventSequenceId|path|[EventSequenceId](#schemaeventsequenceid)|true|The event sequence to append to.|
|tenantId|path|[TenantId](#schematenantid)|true|The tenant to append to.|
|body|body|[AppendEvent](#schemaappendevent)|false|The payload with the details about the event to append.|

<h3 id="post__api_events_store_{microserviceid}_{tenantid}_sequence_{eventsequenceid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_{microserviceId}_{tenantId}_sequence_{eventSequenceId}

`GET /api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}`

*Get events for a specific event sequence in a microservice for a specific tenant.*

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_sequence_{eventsequenceid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|eventSequenceId|path|[EventSequenceId](#schemaeventsequenceid)|true|Event sequence to get for.|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|Microservice to get for.|
|tenantId|query|[TenantId](#schematenantid)|false|Tenant to get for.|
|microserviceId|path|string|true|none|
|tenantId|path|string|true|none|

> Example responses

> 200 Response

```
[{"metadata":{"sequenceNumber":{"value":0},"type":{"id":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"generation":{"value":0},"isPublic":true}},"context":{"eventSourceId":{"value":"string","isSpecified":true},"sequenceNumber":{"value":0},"occurred":"2019-08-24T14:15:22Z","validFrom":"2019-08-24T14:15:22Z","tenantId":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"correlationId":{"value":"string"},"causationId":{"value":"string"},"causedBy":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"observationState":0},"content":{"options":{"propertyNameCaseInsensitive":true},"parent":{},"root":{}}}]
```

```json
[
  {
    "metadata": {
      "sequenceNumber": {
        "value": 0
      },
      "type": {
        "id": {
          "value": "a860a344-d7b2-406e-828e-8d442f23f344"
        },
        "generation": {
          "value": 0
        },
        "isPublic": true
      }
    },
    "context": {
      "eventSourceId": {
        "value": "string",
        "isSpecified": true
      },
      "sequenceNumber": {
        "value": 0
      },
      "occurred": "2019-08-24T14:15:22Z",
      "validFrom": "2019-08-24T14:15:22Z",
      "tenantId": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "correlationId": {
        "value": "string"
      },
      "causationId": {
        "value": "string"
      },
      "causedBy": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "observationState": 0
    },
    "content": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    }
  }
]
```

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_sequence_{eventsequenceid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_sequence_{eventsequenceid}-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[AppendedEventWithJsonAsContent](#schemaappendedeventwithjsonascontent)]|false|none|[Represents an event that has been appended to an event log with the content as JSON.]|
|» metadata|[EventMetadata](#schemaeventmetadata)|false|none|Represents the metadata related to an event.|
|»» sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|»»» value|integer(int64)|false|none|Gets or inits the underlying value for the instance.|
|»» type|[EventType](#schemaeventtype)|false|none|Represents the type of an event.|
|»»» id|[EventTypeId](#schemaeventtypeid)|false|none|Represents the concept of the unique identifier of a type of event.|
|»»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»»» generation|[EventGeneration](#schemaeventgeneration)|false|none|Represents the generation of an Cratis.Events.EventType.|
|»»»» value|integer(int32)|false|none|Gets or inits the underlying value for the instance.|
|»»» isPublic|boolean|false|none|Whether or not the event type is considered a public event.|
|» context|[EventContext](#schemaeventcontext)|false|none|Represents the context in which an event exists - typically what it was appended with.|
|»» eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»»» isSpecified|boolean|false|read-only|Check whether or not the Cratis.Events.EventSourceId is specified.|
|»» sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|»» occurred|string(date-time)|false|none|<see cref="T:System.DateTimeOffset">When</see> it occurred.|
|»» validFrom|string(date-time)|false|none|<see cref="T:System.DateTimeOffset">When</see> event is considered valid from.|
|»» tenantId|[TenantId](#schematenantid)|false|none|Represents the unique identifier of a tenant in the system.|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» correlationId|[CorrelationId](#schemacorrelationid)|false|none|Represents an identifier for correlation.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» causationId|[CausationId](#schemacausationid)|false|none|Represents an identifier for correlation.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» causedBy|[CausedBy](#schemacausedby)|false|none|Represents an identifier of an identity that was the root of a cause.|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» observationState|[EventObservationState](#schemaeventobservationstate)(int32)|false|none|Represents the observation state for an event.|
|» content|[JsonNode](#schemajsonnode)|false|none|none|
|»» options|[JsonNodeOptions](#schemajsonnodeoptions)|false|none|none|
|»»» propertyNameCaseInsensitive|boolean|false|none|none|
|»» parent|[JsonNode](#schemajsonnode)|false|none|none|
|»» root|[JsonNode](#schemajsonnode)|false|none|none|

#### Enumerated Values

|Property|Value|
|---|---|
|observationState|0|
|observationState|1|
|observationState|2|
|observationState|4|
|observationState|8|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_{microserviceId}_{tenantId}_sequence_{eventSequenceId}_histogram

`GET /api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}/histogram`

*Get a histogram of a specific event sequence. PS: Not implemented yet.*

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_sequence_{eventsequenceid}_histogram-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|string|true|none|
|tenantId|path|string|true|none|
|eventSequenceId|path|string|true|none|

> Example responses

> 200 Response

```
[{"date":"2019-08-24T14:15:22Z","count":0}]
```

```json
[
  {
    "date": "2019-08-24T14:15:22Z",
    "count": 0
  }
]
```

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_sequence_{eventsequenceid}_histogram-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_sequence_{eventsequenceid}_histogram-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[EventHistogramEntry](#schemaeventhistogramentry)]|false|none|none|
|» date|string(date-time)|false|none|none|
|» count|integer(int32)|false|none|none|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-eventsequences">EventSequences</h1>

## get__api_events_store_sequences

`GET /api/events/store/sequences`

*Gets all event sequences.*

> Example responses

> 200 Response

```
[{"id":"string","name":"string"}]
```

```json
[
  {
    "id": "string",
    "name": "string"
  }
]
```

<h3 id="get__api_events_store_sequences-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_sequences-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[EventSequenceInformation](#schemaeventsequenceinformation)]|false|none|none|
|» id|string¦null|false|none|none|
|» name|string¦null|false|none|none|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-eventtypes">EventTypes</h1>

## post__api_events_store_{microserviceId}_types

`POST /api/events/store/{microserviceId}/types`

*Register schemas.*

> Body parameter

```json
{
  "types": [
    {
      "type": {
        "id": {
          "value": "a860a344-d7b2-406e-828e-8d442f23f344"
        },
        "generation": {
          "value": 0
        },
        "isPublic": true
      },
      "friendlyName": "string",
      "schema": {
        "options": {
          "propertyNameCaseInsensitive": true
        },
        "parent": {},
        "root": {}
      }
    }
  ]
}
```

<h3 id="post__api_events_store_{microserviceid}_types-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|Cratis.Execution.MicroserviceId to register for.|
|body|body|[RegisterEventTypes](#schemaregistereventtypes)|false|The payload.|

<h3 id="post__api_events_store_{microserviceid}_types-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_{microserviceId}_types

`GET /api/events/store/{microserviceId}/types`

*Gets all event types.*

<h3 id="get__api_events_store_{microserviceid}_types-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|The Cratis.Execution.MicroserviceId to get event types for.|

> Example responses

> 200 Response

```
[{"identifier":"string","name":"string","generations":0}]
```

```json
[
  {
    "identifier": "string",
    "name": "string",
    "generations": 0
  }
]
```

<h3 id="get__api_events_store_{microserviceid}_types-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_{microserviceid}_types-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[EventTypeInformation](#schemaeventtypeinformation)]|false|none|none|
|» identifier|string¦null|false|none|none|
|» name|string¦null|false|none|none|
|» generations|integer(int32)|false|none|none|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_{microserviceId}_types_schemas_{eventTypeId}

`GET /api/events/store/{microserviceId}/types/schemas/{eventTypeId}`

*Gets generation schema for type.*

<h3 id="get__api_events_store_{microserviceid}_types_schemas_{eventtypeid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|The Cratis.Execution.MicroserviceId to get event type for.|
|eventTypeId|path|[EventTypeId](#schemaeventtypeid)|true|Type to get for.|
|microserviceId|path|string|true|none|

> Example responses

> 200 Response

```
[null]
```

```json
[
  null
]
```

<h3 id="get__api_events_store_{microserviceid}_types_schemas_{eventtypeid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_{microserviceid}_types_schemas_{eventtypeid}-responseschema">Response Schema</h3>

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-microservices">Microservices</h1>

## post__api_compliance_microservices

`POST /api/compliance/microservices`

*Add a microservice.*

> Body parameter

```json
{
  "microserviceId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "name": "string"
}
```

<h3 id="post__api_compliance_microservices-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[AddMicroservice](#schemaaddmicroservice)|false|M:Cratis.Chronicle.Domain.Compliance.Microservices.Microservices.AddMicroservice(Cratis.Chronicle.Domain.Compliance.Microservices.AddMicroservice) payload.|

<h3 id="post__api_compliance_microservices-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_configuration_microservices

`GET /api/configuration/microservices`

*Returns all the tenants configured in the kernel.*

> Example responses

> 200 Response

```
[{"id":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"name":"string"}]
```

```json
[
  {
    "id": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "name": "string"
  }
]
```

<h3 id="get__api_configuration_microservices-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_configuration_microservices-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[Microservice](#schemamicroservice)]|false|none|none|
|» id|[MicroserviceId](#schemamicroserviceid)|false|none|Represents the concept of the microservice identifier.|
|»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|» name|string¦null|false|none|none|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_configuration_microservices_{microserviceId}_storage

`GET /api/configuration/microservices/{microserviceId}/storage`

*Get storage configuration for a specific microservice.*

<h3 id="get__api_configuration_microservices_{microserviceid}_storage-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|Cratis.Execution.MicroserviceId for the microservice.|

> Example responses

> 200 Response

```
{"shared":{"property1":{"type":"string","connectionDetails":null},"property2":{"type":"string","connectionDetails":null}},"tenants":{"property1":{"property1":{"type":"string","connectionDetails":null},"property2":{"type":"string","connectionDetails":null}},"property2":{"property1":{"type":"string","connectionDetails":null},"property2":{"type":"string","connectionDetails":null}}}}
```

```json
{
  "shared": {
    "property1": {
      "type": "string",
      "connectionDetails": null
    },
    "property2": {
      "type": "string",
      "connectionDetails": null
    }
  },
  "tenants": {
    "property1": {
      "property1": {
        "type": "string",
        "connectionDetails": null
      },
      "property2": {
        "type": "string",
        "connectionDetails": null
      }
    },
    "property2": {
      "property1": {
        "type": "string",
        "connectionDetails": null
      },
      "property2": {
        "type": "string",
        "connectionDetails": null
      }
    }
  }
}
```

<h3 id="get__api_configuration_microservices_{microserviceid}_storage-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[StorageForMicroservice](#schemastorageformicroservice)|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-observers">Observers</h1>

## post__api_events_store_{microserviceId}_observers_register_{connectionId}

`POST /api/events/store/{microserviceId}/observers/register/{connectionId}`

*Register client observers for a specific microservice and unique connection.*

> Body parameter

```json
[
  {
    "observerId": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "name": {
      "value": "string"
    },
    "eventSequenceId": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "eventTypes": [
      {
        "id": {
          "value": "a860a344-d7b2-406e-828e-8d442f23f344"
        },
        "generation": {
          "value": 0
        },
        "isPublic": true
      }
    ]
  }
]
```

<h3 id="post__api_events_store_{microserviceid}_observers_register_{connectionid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|Cratis.Execution.MicroserviceId to register for.|
|connectionId|path|[ConnectionId](#schemaconnectionid)|true|Cratis.Clients.ConnectionId to register with.|
|body|body|[ClientObserverRegistration](#schemaclientobserverregistration)|false|Collection of Cratis.Observation.ClientObserverRegistration.|

<h3 id="post__api_events_store_{microserviceid}_observers_register_{connectionid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## post__api_events_store_{microserviceId}_observers_{observerId}_rewind

`POST /api/events/store/{microserviceId}/observers/{observerId}/rewind`

*Rewind a specific observer for a microservice and tenant.*

<h3 id="post__api_events_store_{microserviceid}_observers_{observerid}_rewind-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|Cratis.Execution.MicroserviceId the observer is for.|
|tenantId|path|[TenantId](#schematenantid)|true|Cratis.Execution.TenantId the observer is for.|
|observerId|path|[ObserverId](#schemaobserverid)|true|Cratis.Observation.ObserverId to rewind.|

<h3 id="post__api_events_store_{microserviceid}_observers_{observerid}_rewind-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_{microserviceId}_{tenantId}_observers

`GET /api/events/store/{microserviceId}/{tenantId}/observers`

*Get and observe all observers.*

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_observers-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|Cratis.Execution.MicroserviceId the observers are for.|
|tenantId|path|[TenantId](#schematenantid)|true|Cratis.Execution.TenantId the observers are for.|

> Example responses

> 200 Response

```
[[{"id":"string","eventTypes":[{"id":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"generation":{"value":0},"isPublic":true}],"eventSequenceId":{"value":"a860a344-d7b2-406e-828e-8d442f23f344","isEventLog":true,"isOutbox":true,"isInbox":true},"observerId":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"name":{"value":"string"},"type":0,"nextEventSequenceNumber":{"value":0},"lastHandled":{"value":0},"runningState":0,"failedPartitions":[{"eventSourceId":{"value":"string","isSpecified":true},"sequenceNumber":{"value":0},"occurred":"2019-08-24T14:15:22Z","lastAttempt":"2019-08-24T14:15:22Z","attempts":0,"messages":["string"],"stackTrace":"string"}],"recoveringPartitions":[{"eventSourceId":{"value":"string","isSpecified":true},"sequenceNumber":{"value":0},"startedRecoveryAt":"2019-08-24T14:15:22Z"}],"hasFailedPartitions":true,"isRecoveringAnyPartition":true,"isDisconnected":true}]]
```

```json
[
  [
    {
      "id": "string",
      "eventTypes": [
        {
          "id": {
            "value": "a860a344-d7b2-406e-828e-8d442f23f344"
          },
          "generation": {
            "value": 0
          },
          "isPublic": true
        }
      ],
      "eventSequenceId": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344",
        "isEventLog": true,
        "isOutbox": true,
        "isInbox": true
      },
      "observerId": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "name": {
        "value": "string"
      },
      "type": 0,
      "nextEventSequenceNumber": {
        "value": 0
      },
      "lastHandled": {
        "value": 0
      },
      "runningState": 0,
      "failedPartitions": [
        {
          "eventSourceId": {
            "value": "string",
            "isSpecified": true
          },
          "sequenceNumber": {
            "value": 0
          },
          "occurred": "2019-08-24T14:15:22Z",
          "lastAttempt": "2019-08-24T14:15:22Z",
          "attempts": 0,
          "messages": [
            "string"
          ],
          "stackTrace": "string"
        }
      ],
      "recoveringPartitions": [
        {
          "eventSourceId": {
            "value": "string",
            "isSpecified": true
          },
          "sequenceNumber": {
            "value": 0
          },
          "startedRecoveryAt": "2019-08-24T14:15:22Z"
        }
      ],
      "hasFailedPartitions": true,
      "isRecoveringAnyPartition": true,
      "isDisconnected": true
    }
  ]
]
```

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_observers-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_{microserviceid}_{tenantid}_observers-responseschema">Response Schema</h3>

Status Code **200**

*Represents an implementation of Cratis.Applications.Queries.IClientObservable.*

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|» id|string¦null|false|none|Gets or sets the identifier of the observer state.|
|» eventTypes|[[EventType](#schemaeventtype)]¦null|false|none|Gets or sets the event types the observer is observing.|
|»» id|[EventTypeId](#schemaeventtypeid)|false|none|Represents the concept of the unique identifier of a type of event.|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» generation|[EventGeneration](#schemaeventgeneration)|false|none|Represents the generation of an Cratis.Events.EventType.|
|»»» value|integer(int32)|false|none|Gets or inits the underlying value for the instance.|
|»» isPublic|boolean|false|none|Whether or not the event type is considered a public event.|
|» eventSequenceId|[EventSequenceId](#schemaeventsequenceid)|false|none|Represents the unique identifier of an event sequence.|
|»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» isEventLog|boolean|false|read-only|Get whether or not this is the default log event sequence.|
|»» isOutbox|boolean|false|read-only|Get whether or not this is the default outbox event sequence.|
|»» isInbox|boolean|false|read-only|Get whether or not this is the default outbox event sequence.|
|» observerId|[ObserverId](#schemaobserverid)|false|none|Concept that represents the unique identifier of an observer.|
|»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|» name|[ObserverName](#schemaobservername)|false|none|Concept that represents the name of an observer.|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|» type|[ObserverType](#schemaobservertype)(int32)|false|none|Defines the different types of observers.|
|» nextEventSequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|»» value|integer(int64)|false|none|Gets or inits the underlying value for the instance.|
|» lastHandled|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|» runningState|[ObserverRunningState](#schemaobserverrunningstate)(int32)|false|none|Defines the status of an observer.|
|» failedPartitions|[[FailedObserverPartition](#schemafailedobserverpartition)]¦null|false|none|Gets or sets the failed partitions for the observer.|
|»» eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»»» isSpecified|boolean|false|read-only|Check whether or not the Cratis.Events.EventSourceId is specified.|
|»» sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|»» occurred|string(date-time)|false|none|Gets or sets the occurred time of the failure - if any.|
|»» lastAttempt|string(date-time)|false|none|Gets or sets the occurred time of the failure - if any.|
|»» attempts|integer(int32)|false|none|Gets or sets the number of retry attempts it has had.|
|»» messages|[string]¦null|false|none|Gets or sets the message from the failure - if any.|
|»» stackTrace|string¦null|false|none|Gets or sets the stack trace from the failure - if any.|
|» recoveringPartitions|[[RecoveringFailedObserverPartition](#schemarecoveringfailedobserverpartition)]¦null|false|none|Gets or sets the failed partitions for the observer.|
|»» eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|»» sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|»» startedRecoveryAt|string(date-time)|false|none|Gets or sets the occurred time of the failure - if any.|
|» hasFailedPartitions|boolean|false|read-only|Gets whether or not there are any failed partitions.|
|» isRecoveringAnyPartition|boolean|false|read-only|Gets whether or not there are any partitions being recovered.|
|» isDisconnected|boolean|false|read-only|Gets whether or not the observer is in disconnected state. Meaning that there is no subscriber to it.|

#### Enumerated Values

|Property|Value|
|---|---|
|type|0|
|type|1|
|type|2|
|type|3|
|runningState|0|
|runningState|1|
|runningState|2|
|runningState|3|
|runningState|4|
|runningState|5|
|runningState|6|
|runningState|7|
|runningState|8|
|runningState|9|
|runningState|10|
|runningState|11|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-pii">PII</h1>

## post__api_compliance_gdpr_pii

`POST /api/compliance/gdpr/pii`

*Create and register a key.*

> Body parameter

```json
{
  "identifier": {
    "value": "string"
  }
}
```

<h3 id="post__api_compliance_gdpr_pii-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[CreateAndRegisterKeyFor](#schemacreateandregisterkeyfor)|false|M:Cratis.Chronicle.Domain.Compliance.GDPR.PII.CreateAndRegisterKeyFor(Cratis.Chronicle.Domain.Compliance.GDPR.CreateAndRegisterKeyFor) payload.|

<h3 id="post__api_compliance_gdpr_pii-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## post__api_compliance_gdpr_pii_delete

`POST /api/compliance/gdpr/pii/delete`

*Delete PII for a person.*

> Body parameter

```json
{
  "personId": {
    "value": "string"
  }
}
```

<h3 id="post__api_compliance_gdpr_pii_delete-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[DeletePIIForPerson](#schemadeletepiiforperson)|false|M:Cratis.Chronicle.Domain.Compliance.GDPR.PII.DeletePIIForPerson(Cratis.Chronicle.Domain.Compliance.GDPR.DeletePIIForPerson) payload.|

<h3 id="post__api_compliance_gdpr_pii_delete-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-people">People</h1>

## get__api_compliance_gdpr_people

`GET /api/compliance/gdpr/people`

*Get all people.*

> Example responses

> 200 Response

```
[[{"id":{"value":"string"},"socialSecurityNumber":{"value":"string","details":"string"},"firstName":{"value":"string","details":"string"},"lastName":{"value":"string","details":"string"},"address":{"value":"string","details":"string"},"city":{"value":"string","details":"string"},"postalCode":{"value":"string","details":"string"},"country":{"value":"string","details":"string"},"personalInformation":[{"identifier":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"type":{"value":"string"},"value":{"value":"string","details":"string"}}]}]]
```

```json
[
  [
    {
      "id": {
        "value": "string"
      },
      "socialSecurityNumber": {
        "value": "string",
        "details": "string"
      },
      "firstName": {
        "value": "string",
        "details": "string"
      },
      "lastName": {
        "value": "string",
        "details": "string"
      },
      "address": {
        "value": "string",
        "details": "string"
      },
      "city": {
        "value": "string",
        "details": "string"
      },
      "postalCode": {
        "value": "string",
        "details": "string"
      },
      "country": {
        "value": "string",
        "details": "string"
      },
      "personalInformation": [
        {
          "identifier": {
            "value": "a860a344-d7b2-406e-828e-8d442f23f344"
          },
          "type": {
            "value": "string"
          },
          "value": {
            "value": "string",
            "details": "string"
          }
        }
      ]
    }
  ]
]
```

<h3 id="get__api_compliance_gdpr_people-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_compliance_gdpr_people-responseschema">Response Schema</h3>

Status Code **200**

*Represents an implementation of Cratis.Applications.Queries.IClientObservable.*

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|» id|[PersonId](#schemapersonid)|false|none|Represents the concept of a unique identifier that identifies a person.|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|» socialSecurityNumber|[SocialSecurityNumber](#schemasocialsecuritynumber)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» firstName|[FirstName](#schemafirstname)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» lastName|[LastName](#schemalastname)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» address|[Address](#schemaaddress)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» city|[City](#schemacity)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» postalCode|[PostalCode](#schemapostalcode)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» country|[Country](#schemacountry)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» personalInformation|[[PersonalInformation](#schemapersonalinformation)]¦null|false|none|none|
|»» identifier|[PersonalInformationId](#schemapersonalinformationid)|false|none|none|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» type|[PersonalInformationType](#schemapersonalinformationtype)|false|none|none|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» value|[PersonalInformationValue](#schemapersonalinformationvalue)|false|none|none|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»»» details|string¦null|false|read-only|Gets the details for the PII.|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_compliance_gdpr_people_search

`GET /api/compliance/gdpr/people/search`

*Search for people by an arbitrary string.*

<h3 id="get__api_compliance_gdpr_people_search-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|query|query|string|false|String to search for.|

> Example responses

> 200 Response

```
[{"id":{"value":"string"},"socialSecurityNumber":{"value":"string","details":"string"},"firstName":{"value":"string","details":"string"},"lastName":{"value":"string","details":"string"},"address":{"value":"string","details":"string"},"city":{"value":"string","details":"string"},"postalCode":{"value":"string","details":"string"},"country":{"value":"string","details":"string"},"personalInformation":[{"identifier":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"type":{"value":"string"},"value":{"value":"string","details":"string"}}]}]
```

```json
[
  {
    "id": {
      "value": "string"
    },
    "socialSecurityNumber": {
      "value": "string",
      "details": "string"
    },
    "firstName": {
      "value": "string",
      "details": "string"
    },
    "lastName": {
      "value": "string",
      "details": "string"
    },
    "address": {
      "value": "string",
      "details": "string"
    },
    "city": {
      "value": "string",
      "details": "string"
    },
    "postalCode": {
      "value": "string",
      "details": "string"
    },
    "country": {
      "value": "string",
      "details": "string"
    },
    "personalInformation": [
      {
        "identifier": {
          "value": "a860a344-d7b2-406e-828e-8d442f23f344"
        },
        "type": {
          "value": "string"
        },
        "value": {
          "value": "string",
          "details": "string"
        }
      }
    ]
  }
]
```

<h3 id="get__api_compliance_gdpr_people_search-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_compliance_gdpr_people_search-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[Person](#schemaperson)]|false|none|none|
|» id|[PersonId](#schemapersonid)|false|none|Represents the concept of a unique identifier that identifies a person.|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|» socialSecurityNumber|[SocialSecurityNumber](#schemasocialsecuritynumber)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» firstName|[FirstName](#schemafirstname)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» lastName|[LastName](#schemalastname)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» address|[Address](#schemaaddress)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» city|[City](#schemacity)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» postalCode|[PostalCode](#schemapostalcode)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» country|[Country](#schemacountry)|false|none|none|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» details|string¦null|false|read-only|Gets the details for the PII.|
|» personalInformation|[[PersonalInformation](#schemapersonalinformation)]¦null|false|none|none|
|»» identifier|[PersonalInformationId](#schemapersonalinformationid)|false|none|none|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» type|[PersonalInformationType](#schemapersonalinformationtype)|false|none|none|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» value|[PersonalInformationValue](#schemapersonalinformationvalue)|false|none|none|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»»» details|string¦null|false|read-only|Gets the details for the PII.|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-projections">Projections</h1>

## post__api_events_store_{microserviceId}_projections

`POST /api/events/store/{microserviceId}/projections`

*Register projections with pipelines.*

> Body parameter

```json
{
  "projections": [
    {
      "projection": {
        "options": {
          "propertyNameCaseInsensitive": true
        },
        "parent": {},
        "root": {}
      },
      "pipeline": {
        "options": {
          "propertyNameCaseInsensitive": true
        },
        "parent": {},
        "root": {}
      }
    }
  ]
}
```

<h3 id="post__api_events_store_{microserviceid}_projections-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|Cratis.Execution.MicroserviceId to register for.|
|body|body|[RegisterProjections](#schemaregisterprojections)|false|The registrations.|

<h3 id="post__api_events_store_{microserviceid}_projections-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_{microserviceId}_projections

`GET /api/events/store/{microserviceId}/projections`

*Gets all projections.*

<h3 id="get__api_events_store_{microserviceid}_projections-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|The Cratis.Execution.MicroserviceId to get projections for.|

> Example responses

> 200 Response

```
[{"id":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"name":{"value":"string"},"modelName":{"value":"string"}}]
```

```json
[
  {
    "id": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "name": {
      "value": "string"
    },
    "modelName": {
      "value": "string"
    }
  }
]
```

<h3 id="get__api_events_store_{microserviceid}_projections-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_{microserviceid}_projections-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[Projection](#schemaprojection)]|false|none|none|
|» id|[ProjectionId](#schemaprojectionid)|false|none|Represents the unique identifier of a projection.|
|»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|» name|[ProjectionName](#schemaprojectionname)|false|none|Represents the friendly display name of a projection.|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|» modelName|[ModelName](#schemamodelname)|false|none|Represents the friendly display name of a model.|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<aside class="success">
This operation does not require authentication
</aside>

## post__api_events_store_{microserviceId}_projections_immediate_{tenantId}

`POST /api/events/store/{microserviceId}/projections/immediate/{tenantId}`

*Perform an immediate projection.*

> Body parameter

```json
{
  "projectionId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "eventSequenceId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "modelKey": {
    "value": "string"
  },
  "projection": {
    "options": {
      "propertyNameCaseInsensitive": true
    },
    "parent": {},
    "root": {}
  }
}
```

<h3 id="post__api_events_store_{microserviceid}_projections_immediate_{tenantid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|path|[MicroserviceId](#schemamicroserviceid)|true|Cratis.Execution.MicroserviceId to perform for.|
|tenantId|path|[TenantId](#schematenantid)|true|Cratis.Execution.TenantId to perform for.|
|body|body|[ImmediateProjection](#schemaimmediateprojection)|false|The details about the Cratis.API.Projections.Commands.ImmediateProjection.|

> Example responses

> 200 Response

```
{"model":{"property1":{"options":{"propertyNameCaseInsensitive":true},"parent":{},"root":{}},"property2":{"options":{"propertyNameCaseInsensitive":true},"parent":{},"root":{}}},"affectedProperties":[{"path":"string","segments":[{"value":"string"}],"lastSegment":{"value":"string"},"isRoot":true,"isSet":true}],"projectedEventsCount":0}
```

```json
{
  "model": {
    "property1": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    },
    "property2": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    }
  },
  "affectedProperties": [
    {
      "path": "string",
      "segments": [
        {
          "value": "string"
        }
      ],
      "lastSegment": {
        "value": "string"
      },
      "isRoot": true,
      "isSet": true
    }
  ],
  "projectedEventsCount": 0
}
```

<h3 id="post__api_events_store_{microserviceid}_projections_immediate_{tenantid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[ImmediateProjectionResult](#schemaimmediateprojectionresult)|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_{microserviceId}_projections_{projectionId}_collections

`GET /api/events/store/{microserviceId}/projections/{projectionId}/collections`

*Get all collections for projection.*

<h3 id="get__api_events_store_{microserviceid}_projections_{projectionid}_collections-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|The Cratis.Execution.MicroserviceId to get projection collections for.|
|projectionId|path|[ProjectionId](#schemaprojectionid)|true|Id of projection to get for.|
|microserviceId|path|string|true|none|

> Example responses

> 200 Response

```
[{"name":"string","documentCount":0}]
```

```json
[
  {
    "name": "string",
    "documentCount": 0
  }
]
```

<h3 id="get__api_events_store_{microserviceid}_projections_{projectionid}_collections-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_{microserviceid}_projections_{projectionid}_collections-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[ProjectionCollection](#schemaprojectioncollection)]|false|none|none|
|» name|string¦null|false|none|none|
|» documentCount|integer(int32)|false|none|none|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-tenantconfiguration">TenantConfiguration</h1>

## post__api_configuration_tenants_{tenantId}

`POST /api/configuration/tenants/{tenantId}`

*Set a key/value pair configuration for a specific tenant.*

> Body parameter

```json
{
  "key": "string",
  "value": "string"
}
```

<h3 id="post__api_configuration_tenants_{tenantid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|tenantId|path|[TenantId](#schematenantid)|true|Cratis.Execution.TenantId for the tenant to set for.|
|body|body|[StringStringKeyValuePair](#schemastringstringkeyvaluepair)|false|The key value pair to set.|

<h3 id="post__api_configuration_tenants_{tenantid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_configuration_tenants_{tenantId}

`GET /api/configuration/tenants/{tenantId}`

*Returns all the configuration key/value pairs associated with a specific tenant.*

<h3 id="get__api_configuration_tenants_{tenantid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|tenantId|path|[TenantId](#schematenantid)|true|Cratis.Execution.TenantId for the tenant to get for.|

> Example responses

> 200 Response

```
{"property1":"string","property2":"string"}
```

```json
{
  "property1": "string",
  "property2": "string"
}
```

<h3 id="get__api_configuration_tenants_{tenantid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_configuration_tenants_{tenantid}-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|» **additionalProperties**|string|false|none|none|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="cratis-kernel-server-tenants">Tenants</h1>

## get__api_configuration_tenants

`GET /api/configuration/tenants`

*Get all the tenants.*

> Example responses

> 200 Response

```
[{"id":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"name":"string"}]
```

```json
[
  {
    "id": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "name": "string"
  }
]
```

<h3 id="get__api_configuration_tenants-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_configuration_tenants-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[TenantInfo](#schematenantinfo)]|false|none|none|
|» id|[TenantId](#schematenantid)|false|none|Represents the unique identifier of a tenant in the system.|
|»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|» name|string¦null|false|none|none|

<aside class="success">
This operation does not require authentication
</aside>

# Schemas

<h2 id="tocS_AddMicroservice">AddMicroservice</h2>
<!-- backwards compatibility -->
<a id="schemaaddmicroservice"></a>
<a id="schema_AddMicroservice"></a>
<a id="tocSaddmicroservice"></a>
<a id="tocsaddmicroservice"></a>

```json
{
  "microserviceId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "name": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|microserviceId|[MicroserviceId](#schemamicroserviceid)|false|none|Represents the concept of the microservice identifier.|
|name|string¦null|false|none|none|

<h2 id="tocS_Address">Address</h2>
<!-- backwards compatibility -->
<a id="schemaaddress"></a>
<a id="schema_Address"></a>
<a id="tocSaddress"></a>
<a id="tocsaddress"></a>

```json
{
  "value": "string",
  "details": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|details|string¦null|false|read-only|Gets the details for the PII.|

<h2 id="tocS_AppendEvent">AppendEvent</h2>
<!-- backwards compatibility -->
<a id="schemaappendevent"></a>
<a id="schema_AppendEvent"></a>
<a id="tocSappendevent"></a>
<a id="tocsappendevent"></a>

```json
{
  "eventSourceId": {
    "value": "string",
    "isSpecified": true
  },
  "eventType": {
    "id": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "generation": {
      "value": 0
    },
    "isPublic": true
  },
  "content": {
    "property1": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    },
    "property2": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    }
  }
}

```

Represents the payload for appending an event.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|eventType|[EventType](#schemaeventtype)|false|none|Represents the type of an event.|
|content|object¦null|false|none|The content of the event represented as System.Text.Json.Nodes.JsonObject.|
|» **additionalProperties**|[JsonNode](#schemajsonnode)|false|none|none|

<h2 id="tocS_AppendedEvent">AppendedEvent</h2>
<!-- backwards compatibility -->
<a id="schemaappendedevent"></a>
<a id="schema_AppendedEvent"></a>
<a id="tocSappendedevent"></a>
<a id="tocsappendedevent"></a>

```json
{
  "metadata": {
    "sequenceNumber": {
      "value": 0
    },
    "type": {
      "id": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "generation": {
        "value": 0
      },
      "isPublic": true
    }
  },
  "context": {
    "eventSourceId": {
      "value": "string",
      "isSpecified": true
    },
    "sequenceNumber": {
      "value": 0
    },
    "occurred": "2019-08-24T14:15:22Z",
    "validFrom": "2019-08-24T14:15:22Z",
    "tenantId": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "correlationId": {
      "value": "string"
    },
    "causationId": {
      "value": "string"
    },
    "causedBy": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "observationState": 0
  },
  "content": {
    "property1": null,
    "property2": null
  }
}

```

Represents an event that has been appended to an event log.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|metadata|[EventMetadata](#schemaeventmetadata)|false|none|Represents the metadata related to an event.|
|context|[EventContext](#schemaeventcontext)|false|none|Represents the context in which an event exists - typically what it was appended with.|
|content|object¦null|false|none|The content in the form of an System.Dynamic.ExpandoObject.|
|» **additionalProperties**|any|false|none|none|

<h2 id="tocS_AppendedEventWithJsonAsContent">AppendedEventWithJsonAsContent</h2>
<!-- backwards compatibility -->
<a id="schemaappendedeventwithjsonascontent"></a>
<a id="schema_AppendedEventWithJsonAsContent"></a>
<a id="tocSappendedeventwithjsonascontent"></a>
<a id="tocsappendedeventwithjsonascontent"></a>

```json
{
  "metadata": {
    "sequenceNumber": {
      "value": 0
    },
    "type": {
      "id": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "generation": {
        "value": 0
      },
      "isPublic": true
    }
  },
  "context": {
    "eventSourceId": {
      "value": "string",
      "isSpecified": true
    },
    "sequenceNumber": {
      "value": 0
    },
    "occurred": "2019-08-24T14:15:22Z",
    "validFrom": "2019-08-24T14:15:22Z",
    "tenantId": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "correlationId": {
      "value": "string"
    },
    "causationId": {
      "value": "string"
    },
    "causedBy": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "observationState": 0
  },
  "content": {
    "options": {
      "propertyNameCaseInsensitive": true
    },
    "parent": {},
    "root": {}
  }
}

```

Represents an event that has been appended to an event log with the content as JSON.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|metadata|[EventMetadata](#schemaeventmetadata)|false|none|Represents the metadata related to an event.|
|context|[EventContext](#schemaeventcontext)|false|none|Represents the context in which an event exists - typically what it was appended with.|
|content|[JsonNode](#schemajsonnode)|false|none|none|

<h2 id="tocS_CausationId">CausationId</h2>
<!-- backwards compatibility -->
<a id="schemacausationid"></a>
<a id="schema_CausationId"></a>
<a id="tocScausationid"></a>
<a id="tocscausationid"></a>

```json
{
  "value": "string"
}

```

Represents an identifier for correlation.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_CausedBy">CausedBy</h2>
<!-- backwards compatibility -->
<a id="schemacausedby"></a>
<a id="schema_CausedBy"></a>
<a id="tocScausedby"></a>
<a id="tocscausedby"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344"
}

```

Represents an identifier of an identity that was the root of a cause.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_City">City</h2>
<!-- backwards compatibility -->
<a id="schemacity"></a>
<a id="schema_City"></a>
<a id="tocScity"></a>
<a id="tocscity"></a>

```json
{
  "value": "string",
  "details": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|details|string¦null|false|read-only|Gets the details for the PII.|

<h2 id="tocS_ClientInformation">ClientInformation</h2>
<!-- backwards compatibility -->
<a id="schemaclientinformation"></a>
<a id="schema_ClientInformation"></a>
<a id="tocSclientinformation"></a>
<a id="tocsclientinformation"></a>

```json
{
  "clientVersion": "string",
  "advertisedUri": "string"
}

```

Represents the information sent to the Kernel when connecting.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|clientVersion|string¦null|false|none|The version of the client.|
|advertisedUri|string¦null|false|none|The URI that the client is advertised with.|

<h2 id="tocS_ClientObserverRegistration">ClientObserverRegistration</h2>
<!-- backwards compatibility -->
<a id="schemaclientobserverregistration"></a>
<a id="schema_ClientObserverRegistration"></a>
<a id="tocSclientobserverregistration"></a>
<a id="tocsclientobserverregistration"></a>

```json
{
  "observerId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "name": {
    "value": "string"
  },
  "eventSequenceId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344",
    "isEventLog": true,
    "isOutbox": true,
    "isInbox": true
  },
  "eventTypes": [
    {
      "id": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "generation": {
        "value": 0
      },
      "isPublic": true
    }
  ]
}

```

Represents the registration of a single client observer.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|observerId|[ObserverId](#schemaobserverid)|false|none|Concept that represents the unique identifier of an observer.|
|name|[ObserverName](#schemaobservername)|false|none|Concept that represents the name of an observer.|
|eventSequenceId|[EventSequenceId](#schemaeventsequenceid)|false|none|Represents the unique identifier of an event sequence.|
|eventTypes|[[EventType](#schemaeventtype)]¦null|false|none|The type of events the observer is interested in.|

<h2 id="tocS_ConnectionId">ConnectionId</h2>
<!-- backwards compatibility -->
<a id="schemaconnectionid"></a>
<a id="schema_ConnectionId"></a>
<a id="tocSconnectionid"></a>
<a id="tocsconnectionid"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344"
}

```

Represents the unique identifier for a connection for a client.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_CorrelationId">CorrelationId</h2>
<!-- backwards compatibility -->
<a id="schemacorrelationid"></a>
<a id="schema_CorrelationId"></a>
<a id="tocScorrelationid"></a>
<a id="tocscorrelationid"></a>

```json
{
  "value": "string"
}

```

Represents an identifier for correlation.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_Country">Country</h2>
<!-- backwards compatibility -->
<a id="schemacountry"></a>
<a id="schema_Country"></a>
<a id="tocScountry"></a>
<a id="tocscountry"></a>

```json
{
  "value": "string",
  "details": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|details|string¦null|false|read-only|Gets the details for the PII.|

<h2 id="tocS_CreateAndRegisterKeyFor">CreateAndRegisterKeyFor</h2>
<!-- backwards compatibility -->
<a id="schemacreateandregisterkeyfor"></a>
<a id="schema_CreateAndRegisterKeyFor"></a>
<a id="tocScreateandregisterkeyfor"></a>
<a id="tocscreateandregisterkeyfor"></a>

```json
{
  "identifier": {
    "value": "string"
  }
}

```

Encapsulation representing the creation and registration of a key for a specific identifier.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|identifier|[EncryptionKeyIdentifier](#schemaencryptionkeyidentifier)|false|none|Represents the unique identifier of an encryption key.|

<h2 id="tocS_DeletePIIForPerson">DeletePIIForPerson</h2>
<!-- backwards compatibility -->
<a id="schemadeletepiiforperson"></a>
<a id="schema_DeletePIIForPerson"></a>
<a id="tocSdeletepiiforperson"></a>
<a id="tocsdeletepiiforperson"></a>

```json
{
  "personId": {
    "value": "string"
  }
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|personId|[PersonId](#schemapersonid)|false|none|Represents the concept of a unique identifier that identifies a person.|

<h2 id="tocS_EncryptionKeyIdentifier">EncryptionKeyIdentifier</h2>
<!-- backwards compatibility -->
<a id="schemaencryptionkeyidentifier"></a>
<a id="schema_EncryptionKeyIdentifier"></a>
<a id="tocSencryptionkeyidentifier"></a>
<a id="tocsencryptionkeyidentifier"></a>

```json
{
  "value": "string"
}

```

Represents the unique identifier of an encryption key.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_EventContext">EventContext</h2>
<!-- backwards compatibility -->
<a id="schemaeventcontext"></a>
<a id="schema_EventContext"></a>
<a id="tocSeventcontext"></a>
<a id="tocseventcontext"></a>

```json
{
  "eventSourceId": {
    "value": "string",
    "isSpecified": true
  },
  "sequenceNumber": {
    "value": 0
  },
  "occurred": "2019-08-24T14:15:22Z",
  "validFrom": "2019-08-24T14:15:22Z",
  "tenantId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "correlationId": {
    "value": "string"
  },
  "causationId": {
    "value": "string"
  },
  "causedBy": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "observationState": 0
}

```

Represents the context in which an event exists - typically what it was appended with.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|occurred|string(date-time)|false|none|<see cref="T:System.DateTimeOffset">When</see> it occurred.|
|validFrom|string(date-time)|false|none|<see cref="T:System.DateTimeOffset">When</see> event is considered valid from.|
|tenantId|[TenantId](#schematenantid)|false|none|Represents the unique identifier of a tenant in the system.|
|correlationId|[CorrelationId](#schemacorrelationid)|false|none|Represents an identifier for correlation.|
|causationId|[CausationId](#schemacausationid)|false|none|Represents an identifier for correlation.|
|causedBy|[CausedBy](#schemacausedby)|false|none|Represents an identifier of an identity that was the root of a cause.|
|observationState|[EventObservationState](#schemaeventobservationstate)|false|none|Represents the observation state for an event.|

<h2 id="tocS_EventGeneration">EventGeneration</h2>
<!-- backwards compatibility -->
<a id="schemaeventgeneration"></a>
<a id="schema_EventGeneration"></a>
<a id="tocSeventgeneration"></a>
<a id="tocseventgeneration"></a>

```json
{
  "value": 0
}

```

Represents the generation of an Cratis.Events.EventType.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|integer(int32)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_EventHistogramEntry">EventHistogramEntry</h2>
<!-- backwards compatibility -->
<a id="schemaeventhistogramentry"></a>
<a id="schema_EventHistogramEntry"></a>
<a id="tocSeventhistogramentry"></a>
<a id="tocseventhistogramentry"></a>

```json
{
  "date": "2019-08-24T14:15:22Z",
  "count": 0
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|date|string(date-time)|false|none|none|
|count|integer(int32)|false|none|none|

<h2 id="tocS_EventMetadata">EventMetadata</h2>
<!-- backwards compatibility -->
<a id="schemaeventmetadata"></a>
<a id="schema_EventMetadata"></a>
<a id="tocSeventmetadata"></a>
<a id="tocseventmetadata"></a>

```json
{
  "sequenceNumber": {
    "value": 0
  },
  "type": {
    "id": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "generation": {
      "value": 0
    },
    "isPublic": true
  }
}

```

Represents the metadata related to an event.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|type|[EventType](#schemaeventtype)|false|none|Represents the type of an event.|

<h2 id="tocS_EventObservationState">EventObservationState</h2>
<!-- backwards compatibility -->
<a id="schemaeventobservationstate"></a>
<a id="schema_EventObservationState"></a>
<a id="tocSeventobservationstate"></a>
<a id="tocseventobservationstate"></a>

```json
0

```

Represents the observation state for an event.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|integer(int32)|false|none|Represents the observation state for an event.|

#### Enumerated Values

|Property|Value|
|---|---|
|*anonymous*|0|
|*anonymous*|1|
|*anonymous*|2|
|*anonymous*|4|
|*anonymous*|8|

<h2 id="tocS_EventSequenceId">EventSequenceId</h2>
<!-- backwards compatibility -->
<a id="schemaeventsequenceid"></a>
<a id="schema_EventSequenceId"></a>
<a id="tocSeventsequenceid"></a>
<a id="tocseventsequenceid"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344",
  "isEventLog": true,
  "isOutbox": true,
  "isInbox": true
}

```

Represents the unique identifier of an event sequence.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|isEventLog|boolean|false|read-only|Get whether or not this is the default log event sequence.|
|isOutbox|boolean|false|read-only|Get whether or not this is the default outbox event sequence.|
|isInbox|boolean|false|read-only|Get whether or not this is the default outbox event sequence.|

<h2 id="tocS_EventSequenceInformation">EventSequenceInformation</h2>
<!-- backwards compatibility -->
<a id="schemaeventsequenceinformation"></a>
<a id="schema_EventSequenceInformation"></a>
<a id="tocSeventsequenceinformation"></a>
<a id="tocseventsequenceinformation"></a>

```json
{
  "id": "string",
  "name": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|string¦null|false|none|none|
|name|string¦null|false|none|none|

<h2 id="tocS_EventSequenceNumber">EventSequenceNumber</h2>
<!-- backwards compatibility -->
<a id="schemaeventsequencenumber"></a>
<a id="schema_EventSequenceNumber"></a>
<a id="tocSeventsequencenumber"></a>
<a id="tocseventsequencenumber"></a>

```json
{
  "value": 0
}

```

Represents the sequence number within an event log for an event.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|integer(int64)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_EventSourceId">EventSourceId</h2>
<!-- backwards compatibility -->
<a id="schemaeventsourceid"></a>
<a id="schema_EventSourceId"></a>
<a id="tocSeventsourceid"></a>
<a id="tocseventsourceid"></a>

```json
{
  "value": "string",
  "isSpecified": true
}

```

Represents the unique identifier of an instance of an event source.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|isSpecified|boolean|false|read-only|Check whether or not the Cratis.Events.EventSourceId is specified.|

<h2 id="tocS_EventType">EventType</h2>
<!-- backwards compatibility -->
<a id="schemaeventtype"></a>
<a id="schema_EventType"></a>
<a id="tocSeventtype"></a>
<a id="tocseventtype"></a>

```json
{
  "id": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "generation": {
    "value": 0
  },
  "isPublic": true
}

```

Represents the type of an event.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|[EventTypeId](#schemaeventtypeid)|false|none|Represents the concept of the unique identifier of a type of event.|
|generation|[EventGeneration](#schemaeventgeneration)|false|none|Represents the generation of an Cratis.Events.EventType.|
|isPublic|boolean|false|none|Whether or not the event type is considered a public event.|

<h2 id="tocS_EventTypeId">EventTypeId</h2>
<!-- backwards compatibility -->
<a id="schemaeventtypeid"></a>
<a id="schema_EventTypeId"></a>
<a id="tocSeventtypeid"></a>
<a id="tocseventtypeid"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344"
}

```

Represents the concept of the unique identifier of a type of event.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_EventTypeInformation">EventTypeInformation</h2>
<!-- backwards compatibility -->
<a id="schemaeventtypeinformation"></a>
<a id="schema_EventTypeInformation"></a>
<a id="tocSeventtypeinformation"></a>
<a id="tocseventtypeinformation"></a>

```json
{
  "identifier": "string",
  "name": "string",
  "generations": 0
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|identifier|string¦null|false|none|none|
|name|string¦null|false|none|none|
|generations|integer(int32)|false|none|none|

<h2 id="tocS_EventTypeRegistration">EventTypeRegistration</h2>
<!-- backwards compatibility -->
<a id="schemaeventtyperegistration"></a>
<a id="schema_EventTypeRegistration"></a>
<a id="tocSeventtyperegistration"></a>
<a id="tocseventtyperegistration"></a>

```json
{
  "type": {
    "id": {
      "value": "a860a344-d7b2-406e-828e-8d442f23f344"
    },
    "generation": {
      "value": 0
    },
    "isPublic": true
  },
  "friendlyName": "string",
  "schema": {
    "options": {
      "propertyNameCaseInsensitive": true
    },
    "parent": {},
    "root": {}
  }
}

```

Representation of an event type registration.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|type|[EventType](#schemaeventtype)|false|none|Represents the type of an event.|
|friendlyName|string¦null|false|none|A friendly name.|
|schema|[JsonNode](#schemajsonnode)|false|none|none|

<h2 id="tocS_FailedObserverPartition">FailedObserverPartition</h2>
<!-- backwards compatibility -->
<a id="schemafailedobserverpartition"></a>
<a id="schema_FailedObserverPartition"></a>
<a id="tocSfailedobserverpartition"></a>
<a id="tocsfailedobserverpartition"></a>

```json
{
  "eventSourceId": {
    "value": "string",
    "isSpecified": true
  },
  "sequenceNumber": {
    "value": 0
  },
  "occurred": "2019-08-24T14:15:22Z",
  "lastAttempt": "2019-08-24T14:15:22Z",
  "attempts": 0,
  "messages": [
    "string"
  ],
  "stackTrace": "string"
}

```

Represents the state used for failed observers.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|occurred|string(date-time)|false|none|Gets or sets the occurred time of the failure - if any.|
|lastAttempt|string(date-time)|false|none|Gets or sets the occurred time of the failure - if any.|
|attempts|integer(int32)|false|none|Gets or sets the number of retry attempts it has had.|
|messages|[string]¦null|false|none|Gets or sets the message from the failure - if any.|
|stackTrace|string¦null|false|none|Gets or sets the stack trace from the failure - if any.|

<h2 id="tocS_FirstName">FirstName</h2>
<!-- backwards compatibility -->
<a id="schemafirstname"></a>
<a id="schema_FirstName"></a>
<a id="tocSfirstname"></a>
<a id="tocsfirstname"></a>

```json
{
  "value": "string",
  "details": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|details|string¦null|false|read-only|Gets the details for the PII.|

<h2 id="tocS_IPropertyPathSegment">IPropertyPathSegment</h2>
<!-- backwards compatibility -->
<a id="schemaipropertypathsegment"></a>
<a id="schema_IPropertyPathSegment"></a>
<a id="tocSipropertypathsegment"></a>
<a id="tocsipropertypathsegment"></a>

```json
{
  "value": "string"
}

```

Defines a segment within a Cratis.Properties.PropertyPath.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|read-only|Gets the value that represents the segment.|

<h2 id="tocS_ImmediateProjection">ImmediateProjection</h2>
<!-- backwards compatibility -->
<a id="schemaimmediateprojection"></a>
<a id="schema_ImmediateProjection"></a>
<a id="tocSimmediateprojection"></a>
<a id="tocsimmediateprojection"></a>

```json
{
  "projectionId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "eventSequenceId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344",
    "isEventLog": true,
    "isOutbox": true,
    "isInbox": true
  },
  "modelKey": {
    "value": "string",
    "isSpecified": true
  },
  "projection": {
    "options": {
      "propertyNameCaseInsensitive": true
    },
    "parent": {},
    "root": {}
  }
}

```

Represents the payload for performing an immediate projection.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|projectionId|[ProjectionId](#schemaprojectionid)|false|none|Represents the unique identifier of a projection.|
|eventSequenceId|[EventSequenceId](#schemaeventsequenceid)|false|none|Represents the unique identifier of an event sequence.|
|modelKey|[ModelKey](#schemamodelkey)|false|none|Represents the unique identifier of an instance of an event source.|
|projection|[JsonNode](#schemajsonnode)|false|none|none|

<h2 id="tocS_ImmediateProjectionResult">ImmediateProjectionResult</h2>
<!-- backwards compatibility -->
<a id="schemaimmediateprojectionresult"></a>
<a id="schema_ImmediateProjectionResult"></a>
<a id="tocSimmediateprojectionresult"></a>
<a id="tocsimmediateprojectionresult"></a>

```json
{
  "model": {
    "property1": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    },
    "property2": {
      "options": {
        "propertyNameCaseInsensitive": true
      },
      "parent": {},
      "root": {}
    }
  },
  "affectedProperties": [
    {
      "path": "string",
      "segments": [
        {
          "value": "string"
        }
      ],
      "lastSegment": {
        "value": "string"
      },
      "isRoot": true,
      "isSet": true
    }
  ],
  "projectedEventsCount": 0
}

```

Represents the result of an Cratis.Chronicle.Grains.Projections.IImmediateProjection.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|model|object¦null|false|none|The Json representation of the model.|
|» **additionalProperties**|[JsonNode](#schemajsonnode)|false|none|none|
|affectedProperties|[[PropertyPath](#schemapropertypath)]¦null|false|none|Collection of properties that was set.|
|projectedEventsCount|integer(int32)|false|none|Number of events that caused projection.|

<h2 id="tocS_JsonNode">JsonNode</h2>
<!-- backwards compatibility -->
<a id="schemajsonnode"></a>
<a id="schema_JsonNode"></a>
<a id="tocSjsonnode"></a>
<a id="tocsjsonnode"></a>

```json
{
  "options": {
    "propertyNameCaseInsensitive": true
  },
  "parent": {
    "options": {
      "propertyNameCaseInsensitive": true
    },
    "parent": {},
    "root": {}
  },
  "root": {
    "options": {
      "propertyNameCaseInsensitive": true
    },
    "parent": {},
    "root": {}
  }
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|options|[JsonNodeOptions](#schemajsonnodeoptions)|false|none|none|
|parent|[JsonNode](#schemajsonnode)|false|none|none|
|root|[JsonNode](#schemajsonnode)|false|none|none|

<h2 id="tocS_JsonNodeOptions">JsonNodeOptions</h2>
<!-- backwards compatibility -->
<a id="schemajsonnodeoptions"></a>
<a id="schema_JsonNodeOptions"></a>
<a id="tocSjsonnodeoptions"></a>
<a id="tocsjsonnodeoptions"></a>

```json
{
  "propertyNameCaseInsensitive": true
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|propertyNameCaseInsensitive|boolean|false|none|none|

<h2 id="tocS_LastName">LastName</h2>
<!-- backwards compatibility -->
<a id="schemalastname"></a>
<a id="schema_LastName"></a>
<a id="tocSlastname"></a>
<a id="tocslastname"></a>

```json
{
  "value": "string",
  "details": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|details|string¦null|false|read-only|Gets the details for the PII.|

<h2 id="tocS_Microservice">Microservice</h2>
<!-- backwards compatibility -->
<a id="schemamicroservice"></a>
<a id="schema_Microservice"></a>
<a id="tocSmicroservice"></a>
<a id="tocsmicroservice"></a>

```json
{
  "id": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "name": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|[MicroserviceId](#schemamicroserviceid)|false|none|Represents the concept of the microservice identifier.|
|name|string¦null|false|none|none|

<h2 id="tocS_MicroserviceId">MicroserviceId</h2>
<!-- backwards compatibility -->
<a id="schemamicroserviceid"></a>
<a id="schema_MicroserviceId"></a>
<a id="tocSmicroserviceid"></a>
<a id="tocsmicroserviceid"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344"
}

```

Represents the concept of the microservice identifier.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_ModelKey">ModelKey</h2>
<!-- backwards compatibility -->
<a id="schemamodelkey"></a>
<a id="schema_ModelKey"></a>
<a id="tocSmodelkey"></a>
<a id="tocsmodelkey"></a>

```json
{
  "value": "string",
  "isSpecified": true
}

```

Represents the unique identifier of an instance of an event source.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|isSpecified|boolean|false|read-only|Check whether or not the Cratis.Projections.ModelKey is specified.|

<h2 id="tocS_ModelName">ModelName</h2>
<!-- backwards compatibility -->
<a id="schemamodelname"></a>
<a id="schema_ModelName"></a>
<a id="tocSmodelname"></a>
<a id="tocsmodelname"></a>

```json
{
  "value": "string"
}

```

Represents the friendly display name of a model.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_ObserverId">ObserverId</h2>
<!-- backwards compatibility -->
<a id="schemaobserverid"></a>
<a id="schema_ObserverId"></a>
<a id="tocSobserverid"></a>
<a id="tocsobserverid"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344"
}

```

Concept that represents the unique identifier of an observer.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_ObserverName">ObserverName</h2>
<!-- backwards compatibility -->
<a id="schemaobservername"></a>
<a id="schema_ObserverName"></a>
<a id="tocSobservername"></a>
<a id="tocsobservername"></a>

```json
{
  "value": "string"
}

```

Concept that represents the name of an observer.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_ObserverRunningState">ObserverRunningState</h2>
<!-- backwards compatibility -->
<a id="schemaobserverrunningstate"></a>
<a id="schema_ObserverRunningState"></a>
<a id="tocSobserverrunningstate"></a>
<a id="tocsobserverrunningstate"></a>

```json
0

```

Defines the status of an observer.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|integer(int32)|false|none|Defines the status of an observer.|

#### Enumerated Values

|Property|Value|
|---|---|
|*anonymous*|0|
|*anonymous*|1|
|*anonymous*|2|
|*anonymous*|3|
|*anonymous*|4|
|*anonymous*|5|
|*anonymous*|6|
|*anonymous*|7|
|*anonymous*|8|
|*anonymous*|9|
|*anonymous*|10|
|*anonymous*|11|

<h2 id="tocS_ObserverState">ObserverState</h2>
<!-- backwards compatibility -->
<a id="schemaobserverstate"></a>
<a id="schema_ObserverState"></a>
<a id="tocSobserverstate"></a>
<a id="tocsobserverstate"></a>

```json
{
  "id": "string",
  "eventTypes": [
    {
      "id": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "generation": {
        "value": 0
      },
      "isPublic": true
    }
  ],
  "eventSequenceId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344",
    "isEventLog": true,
    "isOutbox": true,
    "isInbox": true
  },
  "observerId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "name": {
    "value": "string"
  },
  "type": 0,
  "nextEventSequenceNumber": {
    "value": 0
  },
  "lastHandled": {
    "value": 0
  },
  "runningState": 0,
  "failedPartitions": [
    {
      "eventSourceId": {
        "value": "string",
        "isSpecified": true
      },
      "sequenceNumber": {
        "value": 0
      },
      "occurred": "2019-08-24T14:15:22Z",
      "lastAttempt": "2019-08-24T14:15:22Z",
      "attempts": 0,
      "messages": [
        "string"
      ],
      "stackTrace": "string"
    }
  ],
  "recoveringPartitions": [
    {
      "eventSourceId": {
        "value": "string",
        "isSpecified": true
      },
      "sequenceNumber": {
        "value": 0
      },
      "startedRecoveryAt": "2019-08-24T14:15:22Z"
    }
  ],
  "hasFailedPartitions": true,
  "isRecoveringAnyPartition": true,
  "isDisconnected": true
}

```

Represents the state used for an observer.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|string¦null|false|none|Gets or sets the identifier of the observer state.|
|eventTypes|[[EventType](#schemaeventtype)]¦null|false|none|Gets or sets the event types the observer is observing.|
|eventSequenceId|[EventSequenceId](#schemaeventsequenceid)|false|none|Represents the unique identifier of an event sequence.|
|observerId|[ObserverId](#schemaobserverid)|false|none|Concept that represents the unique identifier of an observer.|
|name|[ObserverName](#schemaobservername)|false|none|Concept that represents the name of an observer.|
|type|[ObserverType](#schemaobservertype)|false|none|Defines the different types of observers.|
|nextEventSequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|lastHandled|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|runningState|[ObserverRunningState](#schemaobserverrunningstate)|false|none|Defines the status of an observer.|
|failedPartitions|[[FailedObserverPartition](#schemafailedobserverpartition)]¦null|false|none|Gets or sets the failed partitions for the observer.|
|recoveringPartitions|[[RecoveringFailedObserverPartition](#schemarecoveringfailedobserverpartition)]¦null|false|none|Gets or sets the failed partitions for the observer.|
|hasFailedPartitions|boolean|false|read-only|Gets whether or not there are any failed partitions.|
|isRecoveringAnyPartition|boolean|false|read-only|Gets whether or not there are any partitions being recovered.|
|isDisconnected|boolean|false|read-only|Gets whether or not the observer is in disconnected state. Meaning that there is no subscriber to it.|

<h2 id="tocS_ObserverType">ObserverType</h2>
<!-- backwards compatibility -->
<a id="schemaobservertype"></a>
<a id="schema_ObserverType"></a>
<a id="tocSobservertype"></a>
<a id="tocsobservertype"></a>

```json
0

```

Defines the different types of observers.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|integer(int32)|false|none|Defines the different types of observers.|

#### Enumerated Values

|Property|Value|
|---|---|
|*anonymous*|0|
|*anonymous*|1|
|*anonymous*|2|
|*anonymous*|3|

<h2 id="tocS_Person">Person</h2>
<!-- backwards compatibility -->
<a id="schemaperson"></a>
<a id="schema_Person"></a>
<a id="tocSperson"></a>
<a id="tocsperson"></a>

```json
{
  "id": {
    "value": "string"
  },
  "socialSecurityNumber": {
    "value": "string",
    "details": "string"
  },
  "firstName": {
    "value": "string",
    "details": "string"
  },
  "lastName": {
    "value": "string",
    "details": "string"
  },
  "address": {
    "value": "string",
    "details": "string"
  },
  "city": {
    "value": "string",
    "details": "string"
  },
  "postalCode": {
    "value": "string",
    "details": "string"
  },
  "country": {
    "value": "string",
    "details": "string"
  },
  "personalInformation": [
    {
      "identifier": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344"
      },
      "type": {
        "value": "string"
      },
      "value": {
        "value": "string",
        "details": "string"
      }
    }
  ]
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|[PersonId](#schemapersonid)|false|none|Represents the concept of a unique identifier that identifies a person.|
|socialSecurityNumber|[SocialSecurityNumber](#schemasocialsecuritynumber)|false|none|none|
|firstName|[FirstName](#schemafirstname)|false|none|none|
|lastName|[LastName](#schemalastname)|false|none|none|
|address|[Address](#schemaaddress)|false|none|none|
|city|[City](#schemacity)|false|none|none|
|postalCode|[PostalCode](#schemapostalcode)|false|none|none|
|country|[Country](#schemacountry)|false|none|none|
|personalInformation|[[PersonalInformation](#schemapersonalinformation)]¦null|false|none|none|

<h2 id="tocS_PersonId">PersonId</h2>
<!-- backwards compatibility -->
<a id="schemapersonid"></a>
<a id="schema_PersonId"></a>
<a id="tocSpersonid"></a>
<a id="tocspersonid"></a>

```json
{
  "value": "string"
}

```

Represents the concept of a unique identifier that identifies a person.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_PersonalInformation">PersonalInformation</h2>
<!-- backwards compatibility -->
<a id="schemapersonalinformation"></a>
<a id="schema_PersonalInformation"></a>
<a id="tocSpersonalinformation"></a>
<a id="tocspersonalinformation"></a>

```json
{
  "identifier": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "type": {
    "value": "string"
  },
  "value": {
    "value": "string",
    "details": "string"
  }
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|identifier|[PersonalInformationId](#schemapersonalinformationid)|false|none|none|
|type|[PersonalInformationType](#schemapersonalinformationtype)|false|none|none|
|value|[PersonalInformationValue](#schemapersonalinformationvalue)|false|none|none|

<h2 id="tocS_PersonalInformationId">PersonalInformationId</h2>
<!-- backwards compatibility -->
<a id="schemapersonalinformationid"></a>
<a id="schema_PersonalInformationId"></a>
<a id="tocSpersonalinformationid"></a>
<a id="tocspersonalinformationid"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_PersonalInformationType">PersonalInformationType</h2>
<!-- backwards compatibility -->
<a id="schemapersonalinformationtype"></a>
<a id="schema_PersonalInformationType"></a>
<a id="tocSpersonalinformationtype"></a>
<a id="tocspersonalinformationtype"></a>

```json
{
  "value": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_PersonalInformationValue">PersonalInformationValue</h2>
<!-- backwards compatibility -->
<a id="schemapersonalinformationvalue"></a>
<a id="schema_PersonalInformationValue"></a>
<a id="tocSpersonalinformationvalue"></a>
<a id="tocspersonalinformationvalue"></a>

```json
{
  "value": "string",
  "details": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|details|string¦null|false|read-only|Gets the details for the PII.|

<h2 id="tocS_PostalCode">PostalCode</h2>
<!-- backwards compatibility -->
<a id="schemapostalcode"></a>
<a id="schema_PostalCode"></a>
<a id="tocSpostalcode"></a>
<a id="tocspostalcode"></a>

```json
{
  "value": "string",
  "details": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|details|string¦null|false|read-only|Gets the details for the PII.|

<h2 id="tocS_Projection">Projection</h2>
<!-- backwards compatibility -->
<a id="schemaprojection"></a>
<a id="schema_Projection"></a>
<a id="tocSprojection"></a>
<a id="tocsprojection"></a>

```json
{
  "id": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "name": {
    "value": "string"
  },
  "modelName": {
    "value": "string"
  }
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|[ProjectionId](#schemaprojectionid)|false|none|Represents the unique identifier of a projection.|
|name|[ProjectionName](#schemaprojectionname)|false|none|Represents the friendly display name of a projection.|
|modelName|[ModelName](#schemamodelname)|false|none|Represents the friendly display name of a model.|

<h2 id="tocS_ProjectionCollection">ProjectionCollection</h2>
<!-- backwards compatibility -->
<a id="schemaprojectioncollection"></a>
<a id="schema_ProjectionCollection"></a>
<a id="tocSprojectioncollection"></a>
<a id="tocsprojectioncollection"></a>

```json
{
  "name": "string",
  "documentCount": 0
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string¦null|false|none|none|
|documentCount|integer(int32)|false|none|none|

<h2 id="tocS_ProjectionId">ProjectionId</h2>
<!-- backwards compatibility -->
<a id="schemaprojectionid"></a>
<a id="schema_ProjectionId"></a>
<a id="tocSprojectionid"></a>
<a id="tocsprojectionid"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344"
}

```

Represents the unique identifier of a projection.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_ProjectionName">ProjectionName</h2>
<!-- backwards compatibility -->
<a id="schemaprojectionname"></a>
<a id="schema_ProjectionName"></a>
<a id="tocSprojectionname"></a>
<a id="tocsprojectionname"></a>

```json
{
  "value": "string"
}

```

Represents the friendly display name of a projection.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_ProjectionRegistration">ProjectionRegistration</h2>
<!-- backwards compatibility -->
<a id="schemaprojectionregistration"></a>
<a id="schema_ProjectionRegistration"></a>
<a id="tocSprojectionregistration"></a>
<a id="tocsprojectionregistration"></a>

```json
{
  "projection": {
    "options": {
      "propertyNameCaseInsensitive": true
    },
    "parent": {},
    "root": {}
  },
  "pipeline": {
    "options": {
      "propertyNameCaseInsensitive": true
    },
    "parent": {},
    "root": {}
  }
}

```

Represents a single projection registration.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|projection|[JsonNode](#schemajsonnode)|false|none|none|
|pipeline|[JsonNode](#schemajsonnode)|false|none|none|

<h2 id="tocS_PropertyPath">PropertyPath</h2>
<!-- backwards compatibility -->
<a id="schemapropertypath"></a>
<a id="schema_PropertyPath"></a>
<a id="tocSpropertypath"></a>
<a id="tocspropertypath"></a>

```json
{
  "path": "string",
  "segments": [
    {
      "value": "string"
    }
  ],
  "lastSegment": {
    "value": "string"
  },
  "isRoot": true,
  "isSet": true
}

```

Represents an encapsulation of a property in the system - used for accessing properties on objects.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|path|string¦null|false|none|Gets the full path of the property.|
|segments|[[IPropertyPathSegment](#schemaipropertypathsegment)]¦null|false|read-only|Gets the segments the full property path consists of.|
|lastSegment|[IPropertyPathSegment](#schemaipropertypathsegment)|false|none|Defines a segment within a Cratis.Properties.PropertyPath.|
|isRoot|boolean|false|read-only|Gets whether or not this is the root path.|
|isSet|boolean|false|read-only|Gets whether or not the value is set.|

<h2 id="tocS_RecoveringFailedObserverPartition">RecoveringFailedObserverPartition</h2>
<!-- backwards compatibility -->
<a id="schemarecoveringfailedobserverpartition"></a>
<a id="schema_RecoveringFailedObserverPartition"></a>
<a id="tocSrecoveringfailedobserverpartition"></a>
<a id="tocsrecoveringfailedobserverpartition"></a>

```json
{
  "eventSourceId": {
    "value": "string",
    "isSpecified": true
  },
  "sequenceNumber": {
    "value": 0
  },
  "startedRecoveryAt": "2019-08-24T14:15:22Z"
}

```

Represents the state used when recovering a failed observer partition.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|startedRecoveryAt|string(date-time)|false|none|Gets or sets the occurred time of the failure - if any.|

<h2 id="tocS_RegisterEventTypes">RegisterEventTypes</h2>
<!-- backwards compatibility -->
<a id="schemaregistereventtypes"></a>
<a id="schema_RegisterEventTypes"></a>
<a id="tocSregistereventtypes"></a>
<a id="tocsregistereventtypes"></a>

```json
{
  "types": [
    {
      "type": {
        "id": {
          "value": "a860a344-d7b2-406e-828e-8d442f23f344"
        },
        "generation": {
          "value": 0
        },
        "isPublic": true
      },
      "friendlyName": "string",
      "schema": {
        "options": {
          "propertyNameCaseInsensitive": true
        },
        "parent": {},
        "root": {}
      }
    }
  ]
}

```

Payload for registering multiple event types.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|types|[[EventTypeRegistration](#schemaeventtyperegistration)]¦null|false|none|Collection of Cratis.API.Projections.Commands.EventTypeRegistration.|

<h2 id="tocS_RegisterProjections">RegisterProjections</h2>
<!-- backwards compatibility -->
<a id="schemaregisterprojections"></a>
<a id="schema_RegisterProjections"></a>
<a id="tocSregisterprojections"></a>
<a id="tocsregisterprojections"></a>

```json
{
  "projections": [
    {
      "projection": {
        "options": {
          "propertyNameCaseInsensitive": true
        },
        "parent": {},
        "root": {}
      },
      "pipeline": {
        "options": {
          "propertyNameCaseInsensitive": true
        },
        "parent": {},
        "root": {}
      }
    }
  ]
}

```

Represents the payload for registering projections.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|projections|[[ProjectionRegistration](#schemaprojectionregistration)]¦null|false|none|Collection of Cratis.API.Projections.Commands.ProjectionRegistration.|

<h2 id="tocS_SocialSecurityNumber">SocialSecurityNumber</h2>
<!-- backwards compatibility -->
<a id="schemasocialsecuritynumber"></a>
<a id="schema_SocialSecurityNumber"></a>
<a id="tocSsocialsecuritynumber"></a>
<a id="tocssocialsecuritynumber"></a>

```json
{
  "value": "string",
  "details": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|details|string¦null|false|read-only|Gets the details for the PII.|

<h2 id="tocS_StorageForMicroservice">StorageForMicroservice</h2>
<!-- backwards compatibility -->
<a id="schemastorageformicroservice"></a>
<a id="schema_StorageForMicroservice"></a>
<a id="tocSstorageformicroservice"></a>
<a id="tocsstorageformicroservice"></a>

```json
{
  "shared": {
    "property1": {
      "type": "string",
      "connectionDetails": null
    },
    "property2": {
      "type": "string",
      "connectionDetails": null
    }
  },
  "tenants": {
    "property1": {
      "property1": {
        "type": "string",
        "connectionDetails": null
      },
      "property2": {
        "type": "string",
        "connectionDetails": null
      }
    },
    "property2": {
      "property1": {
        "type": "string",
        "connectionDetails": null
      },
      "property2": {
        "type": "string",
        "connectionDetails": null
      }
    }
  }
}

```

Represents all storage configurations for all <see cref="T:Cratis.Execution.MicroserviceId">microservices</see> in the system.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|shared|object¦null|false|none|The shared database connection configurations for the microservice.|
|» **additionalProperties**|[StorageType](#schemastoragetype)|false|none|Represents the configuration for a specific shared storage type.|
|tenants|object¦null|false|none|The tenant specific configuration.|
|» **additionalProperties**|object|false|none|Represents the shared storage configuration for all <see cref="T:Cratis.Configuration.StorageType">storage types</see> within the system.|
|»» **additionalProperties**|[StorageType](#schemastoragetype)|false|none|Represents the configuration for a specific shared storage type.|

<h2 id="tocS_StorageType">StorageType</h2>
<!-- backwards compatibility -->
<a id="schemastoragetype"></a>
<a id="schema_StorageType"></a>
<a id="tocSstoragetype"></a>
<a id="tocsstoragetype"></a>

```json
{
  "type": "string",
  "connectionDetails": null
}

```

Represents the configuration for a specific shared storage type.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|type|string¦null|false|none|The type of storage used.|
|connectionDetails|any|false|none|Gets the provider type specific connection details.|

<h2 id="tocS_StringStringKeyValuePair">StringStringKeyValuePair</h2>
<!-- backwards compatibility -->
<a id="schemastringstringkeyvaluepair"></a>
<a id="schema_StringStringKeyValuePair"></a>
<a id="tocSstringstringkeyvaluepair"></a>
<a id="tocsstringstringkeyvaluepair"></a>

```json
{
  "key": "string",
  "value": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|key|string¦null|false|none|none|
|value|string¦null|false|none|none|

<h2 id="tocS_TenantId">TenantId</h2>
<!-- backwards compatibility -->
<a id="schematenantid"></a>
<a id="schema_TenantId"></a>
<a id="tocStenantid"></a>
<a id="tocstenantid"></a>

```json
{
  "value": "a860a344-d7b2-406e-828e-8d442f23f344"
}

```

Represents the unique identifier of a tenant in the system.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|

<h2 id="tocS_TenantInfo">TenantInfo</h2>
<!-- backwards compatibility -->
<a id="schematenantinfo"></a>
<a id="schema_TenantInfo"></a>
<a id="tocStenantinfo"></a>
<a id="tocstenantinfo"></a>

```json
{
  "id": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344"
  },
  "name": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|[TenantId](#schematenantid)|false|none|Represents the unique identifier of a tenant in the system.|
|name|string¦null|false|none|none|

