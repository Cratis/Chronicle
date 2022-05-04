---
title: Aksio.Cratis.Server v1.0
language_tabs: []
toc_footers: []
includes: []
search: true
highlight_theme: darkula
headingLevel: 2

---

<!-- Generator: Widdershins v4.0.1 -->

<h1 id="aksio-cratis-server">Aksio.Cratis.Server v1.0</h1>

> Scroll down for example requests and responses.

<h1 id="aksio-cratis-server-allconfiguration">AllConfiguration</h1>

## get__api_configuration_tenants

`GET /api/configuration/tenants`

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

<h3 id="get__api_configuration_tenants-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_configuration_tenants-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[Tenant](#schematenant)]|false|none|none|
|» id|[TenantId](#schematenantid)|false|none|Represents the unique identifier of a tenant in the system.|
|»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|» name|string¦null|false|none|none|

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

<h1 id="aksio-cratis-server-eventsequence">EventSequence</h1>

## post__api_events_store_sequence_{eventSourceId}_{eventTypeId}_{eventGeneration}

`POST /api/events/store/sequence/{eventSourceId}/{eventTypeId}/{eventGeneration}`

*Appends an event to the event log.*

<h3 id="post__api_events_store_sequence_{eventsourceid}_{eventtypeid}_{eventgeneration}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|eventSourceId|path|[EventSourceId](#schemaeventsourceid)|true|EventSource to append for.|
|eventTypeId|path|[EventTypeId](#schemaeventtypeid)|true|Type of event to append.|
|eventGeneration|path|[EventGeneration](#schemaeventgeneration)|true|Generation of the event to append.|

<h3 id="post__api_events_store_sequence_{eventsourceid}_{eventtypeid}_{eventgeneration}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_sequence_{eventSequenceId}

`GET /api/events/store/sequence/{eventSequenceId}`

*Get events for a specific event sequence in a microservice for a specific tenant.*

<h3 id="get__api_events_store_sequence_{eventsequenceid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|eventSequenceId|path|[EventSequenceId](#schemaeventsequenceid)|true|Event sequence to get for.|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|Microservice to get for.|
|tenantId|query|[TenantId](#schematenantid)|false|Tenant to get for.|

> Example responses

> 200 Response

```
[{"metadata":{"sequenceNumber":{"value":0},"type":{"id":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"generation":{"value":0}}},"context":{"eventSourceId":{"value":"string","isSpecified":true},"occurred":"2019-08-24T14:15:22Z","validFrom":"2019-08-24T14:15:22Z","tenantId":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"correlationId":{"value":"string"},"causationId":{"value":"string"},"causedBy":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"observationState":0},"content":{"property1":{"options":{"propertyNameCaseInsensitive":true},"parent":{},"root":{}},"property2":{"options":{"propertyNameCaseInsensitive":true},"parent":{},"root":{}}}}]
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
        }
      }
    },
    "context": {
      "eventSourceId": {
        "value": "string",
        "isSpecified": true
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
]
```

<h3 id="get__api_events_store_sequence_{eventsequenceid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_sequence_{eventsequenceid}-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[AppendedEvent](#schemaappendedevent)]|false|none|[Represents an event that has been appended to an event log.]|
|» metadata|[EventMetadata](#schemaeventmetadata)|false|none|Represents the metadata related to an event.|
|»» sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|»»» value|integer(int64)|false|none|Gets or inits the underlying value for the instance.|
|»» type|[EventType](#schemaeventtype)|false|none|Represents the type of an event.|
|»»» id|[EventTypeId](#schemaeventtypeid)|false|none|Represents the concept of the unique identifier of a type of event.|
|»»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»»» generation|[EventGeneration](#schemaeventgeneration)|false|none|Represents the generation of an Aksio.Cratis.Events.EventType.|
|»»»» value|integer(int32)|false|none|Gets or inits the underlying value for the instance.|
|» context|[EventContext](#schemaeventcontext)|false|none|Represents the context in which an event exists - typically what it was appended with.|
|»» eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»»» isSpecified|boolean|false|read-only|Check whether or not the Aksio.Cratis.Events.EventSourceId is specified.|
|»» occurred|string(date-time)|false|none|none|
|»» validFrom|string(date-time)|false|none|none|
|»» tenantId|[TenantId](#schematenantid)|false|none|Represents the unique identifier of a tenant in the system.|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» correlationId|[CorrelationId](#schemacorrelationid)|false|none|Represents an identifier for correlation.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» causationId|[CausationId](#schemacausationid)|false|none|Represents an identifier for correlation.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» causedBy|[CausedBy](#schemacausedby)|false|none|Represents an identifier of an identity that was the root of a cause.|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» observationState|[EventObservationState](#schemaeventobservationstate)(int32)|false|none|Represents the observation state for an event.|
|» content|object¦null|false|none|none|
|»» **additionalProperties**|[JsonNode](#schemajsonnode)|false|none|none|
|»»» options|[JsonNodeOptions](#schemajsonnodeoptions)|false|none|none|
|»»»» propertyNameCaseInsensitive|boolean|false|none|none|
|»»» parent|[JsonNode](#schemajsonnode)|false|none|none|
|»»» root|[JsonNode](#schemajsonnode)|false|none|none|

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

## get__api_events_store_sequence_{eventSequenceId}_count

`GET /api/events/store/sequence/{eventSequenceId}/count`

*Count number of events in an event sequence. PS: Not implemented yet.*

<h3 id="get__api_events_store_sequence_{eventsequenceid}_count-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|eventSequenceId|path|string|true|none|

> Example responses

> 200 Response

```
0
```

```json
0
```

<h3 id="get__api_events_store_sequence_{eventsequenceid}_count-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|integer|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_sequence_histogram

`GET /api/events/store/sequence/histogram`

*Get a histogram of a specific event sequence. PS: Not implemented yet.*

<h3 id="get__api_events_store_sequence_histogram-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|eventSequenceId|path|[EventSequenceId](#schemaeventsequenceid)|true|Event sequence to get for.|

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

<h3 id="get__api_events_store_sequence_histogram-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_sequence_histogram-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[EventHistogramEntry](#schemaeventhistogramentry)]|false|none|none|
|» date|string(date-time)|false|none|none|
|» count|integer(int32)|false|none|none|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_sequence_{eventSequenceId}_{eventSourceId}

`GET /api/events/store/sequence/{eventSequenceId}/{eventSourceId}`

*Find events for a specific event source id. PS: Not implemented yet.*

<h3 id="get__api_events_store_sequence_{eventsequenceid}_{eventsourceid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|eventSequenceId|path|[EventSequenceId](#schemaeventsequenceid)|true|Event sequence to find events in.|
|eventSourceId|path|[EventSourceId](#schemaeventsourceid)|true|Event source to get for.|

> Example responses

> 200 Response

```
[{"metadata":{"sequenceNumber":{"value":0},"type":{"id":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"generation":{"value":0}}},"context":{"eventSourceId":{"value":"string","isSpecified":true},"occurred":"2019-08-24T14:15:22Z","validFrom":"2019-08-24T14:15:22Z","tenantId":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"correlationId":{"value":"string"},"causationId":{"value":"string"},"causedBy":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"observationState":0},"content":{"property1":{"options":{"propertyNameCaseInsensitive":true},"parent":{},"root":{}},"property2":{"options":{"propertyNameCaseInsensitive":true},"parent":{},"root":{}}}}]
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
        }
      }
    },
    "context": {
      "eventSourceId": {
        "value": "string",
        "isSpecified": true
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
]
```

<h3 id="get__api_events_store_sequence_{eventsequenceid}_{eventsourceid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_sequence_{eventsequenceid}_{eventsourceid}-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[AppendedEvent](#schemaappendedevent)]|false|none|[Represents an event that has been appended to an event log.]|
|» metadata|[EventMetadata](#schemaeventmetadata)|false|none|Represents the metadata related to an event.|
|»» sequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|»»» value|integer(int64)|false|none|Gets or inits the underlying value for the instance.|
|»» type|[EventType](#schemaeventtype)|false|none|Represents the type of an event.|
|»»» id|[EventTypeId](#schemaeventtypeid)|false|none|Represents the concept of the unique identifier of a type of event.|
|»»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»»» generation|[EventGeneration](#schemaeventgeneration)|false|none|Represents the generation of an Aksio.Cratis.Events.EventType.|
|»»»» value|integer(int32)|false|none|Gets or inits the underlying value for the instance.|
|» context|[EventContext](#schemaeventcontext)|false|none|Represents the context in which an event exists - typically what it was appended with.|
|»» eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»»» isSpecified|boolean|false|read-only|Check whether or not the Aksio.Cratis.Events.EventSourceId is specified.|
|»» occurred|string(date-time)|false|none|none|
|»» validFrom|string(date-time)|false|none|none|
|»» tenantId|[TenantId](#schematenantid)|false|none|Represents the unique identifier of a tenant in the system.|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» correlationId|[CorrelationId](#schemacorrelationid)|false|none|Represents an identifier for correlation.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» causationId|[CausationId](#schemacausationid)|false|none|Represents an identifier for correlation.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»» causedBy|[CausedBy](#schemacausedby)|false|none|Represents an identifier of an identity that was the root of a cause.|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» observationState|[EventObservationState](#schemaeventobservationstate)(int32)|false|none|Represents the observation state for an event.|
|» content|object¦null|false|none|none|
|»» **additionalProperties**|[JsonNode](#schemajsonnode)|false|none|none|
|»»» options|[JsonNodeOptions](#schemajsonnodeoptions)|false|none|none|
|»»»» propertyNameCaseInsensitive|boolean|false|none|none|
|»»» parent|[JsonNode](#schemajsonnode)|false|none|none|
|»»» root|[JsonNode](#schemajsonnode)|false|none|none|

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

<h1 id="aksio-cratis-server-eventsequences">EventSequences</h1>

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

<h1 id="aksio-cratis-server-eventtypes">EventTypes</h1>

## get__api_events_store_types

`GET /api/events/store/types`

*Gets all event types.*

<h3 id="get__api_events_store_types-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|The Aksio.Cratis.Execution.MicroserviceId to get event types for.|

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

<h3 id="get__api_events_store_types-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_types-responseschema">Response Schema</h3>

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

## get__api_events_store_types_schemas_{eventTypeId}

`GET /api/events/store/types/schemas/{eventTypeId}`

*Gets generation schema for type.*

<h3 id="get__api_events_store_types_schemas_{eventtypeid}-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|The Aksio.Cratis.Execution.MicroserviceId to get event type for.|
|eventTypeId|path|[EventTypeId](#schemaeventtypeid)|true|Type to get for.|

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

<h3 id="get__api_events_store_types_schemas_{eventtypeid}-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_types_schemas_{eventtypeid}-responseschema">Response Schema</h3>

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="aksio-cratis-server-microservices">Microservices</h1>

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
|body|body|[AddMicroservice](#schemaaddmicroservice)|false|M:Aksio.Cratis.Compliance.Domain.Microservices.Microservices.AddMicroservice(Aksio.Cratis.Compliance.Domain.Microservices.AddMicroservice) payload.|

<h3 id="post__api_compliance_microservices-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="aksio-cratis-server-observers">Observers</h1>

## post__api_events_store_observers_{observerId}_rewind

`POST /api/events/store/observers/{observerId}/rewind`

*Rewind a specific observer for a microservice and tenant.*

<h3 id="post__api_events_store_observers_{observerid}_rewind-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|observerId|path|[ObserverId](#schemaobserverid)|true|Aksio.Cratis.Events.Store.Observation.ObserverId to rewind.|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|Aksio.Cratis.Execution.MicroserviceId the observer is for.|
|tenantId|query|[TenantId](#schematenantid)|false|Aksio.Cratis.Execution.TenantId the observer is for.|

<h3 id="post__api_events_store_observers_{observerid}_rewind-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

## get__api_events_store_observers

`GET /api/events/store/observers`

*Get and observe all observers.*

<h3 id="get__api_events_store_observers-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|Aksio.Cratis.Execution.MicroserviceId the observers are for.|
|tenantId|query|[TenantId](#schematenantid)|false|Aksio.Cratis.Execution.TenantId the observers are for.|

> Example responses

> 200 Response

```
[[{"id":"string","eventTypes":[{"id":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"generation":{"value":0}}],"eventSequenceId":{"value":"a860a344-d7b2-406e-828e-8d442f23f344","isEventLog":true,"isOutbox":true},"observerId":{"value":"a860a344-d7b2-406e-828e-8d442f23f344"},"name":{"value":"string"},"type":0,"nextEventSequenceNumber":{"value":0},"lastHandled":{"value":0},"runningState":0,"currentNamespace":{"value":"string"},"failedPartitions":[{"eventSourceId":{"value":"string","isSpecified":true},"sequenceNumber":{"value":0},"occurred":"2019-08-24T14:15:22Z","lastAttempt":"2019-08-24T14:15:22Z","attempts":0,"messages":["string"],"stackTrace":"string"}],"recoveringPartitions":[{"eventSourceId":{"value":"string","isSpecified":true},"sequenceNumber":{"value":0},"startedRecoveryAt":"2019-08-24T14:15:22Z"}],"hasFailedPartitions":true,"isRecoveringAnyPartition":true}]]
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
          }
        }
      ],
      "eventSequenceId": {
        "value": "a860a344-d7b2-406e-828e-8d442f23f344",
        "isEventLog": true,
        "isOutbox": true
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
      "currentNamespace": {
        "value": "string"
      },
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
      "isRecoveringAnyPartition": true
    }
  ]
]
```

<h3 id="get__api_events_store_observers-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_observers-responseschema">Response Schema</h3>

Status Code **200**

*Represents an implementation of Aksio.Cratis.Applications.Queries.IClientObservable.*

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|» id|string¦null|false|none|Gets or sets the identifier of the observer state.|
|» eventTypes|[[EventType](#schemaeventtype)]¦null|false|none|Gets or sets the event types the observer is observing.|
|»» id|[EventTypeId](#schemaeventtypeid)|false|none|Represents the concept of the unique identifier of a type of event.|
|»»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» generation|[EventGeneration](#schemaeventgeneration)|false|none|Represents the generation of an Aksio.Cratis.Events.EventType.|
|»»» value|integer(int32)|false|none|Gets or inits the underlying value for the instance.|
|» eventSequenceId|[EventSequenceId](#schemaeventsequenceid)|false|none|Represents the unique identifier of an event sequence.|
|»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|»» isEventLog|boolean|false|read-only|Get whether or not this is the default log event sequence.|
|»» isOutbox|boolean|false|read-only|Get whether or not this is the default outbox event sequence.|
|» observerId|[ObserverId](#schemaobserverid)|false|none|Concept that represents the unique identifier of an observer.|
|»» value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|» name|[ObserverName](#schemaobservername)|false|none|Concept that represents the name of an observer.|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|» type|[ObserverType](#schemaobservertype)(int32)|false|none|Defines the different types of observers.|
|» nextEventSequenceNumber|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|»» value|integer(int64)|false|none|Gets or inits the underlying value for the instance.|
|» lastHandled|[EventSequenceNumber](#schemaeventsequencenumber)|false|none|Represents the sequence number within an event log for an event.|
|» runningState|[ObserverRunningState](#schemaobserverrunningstate)(int32)|false|none|Defines the status of an observer.|
|» currentNamespace|[ObserverNamespace](#schemaobservernamespace)|false|none|Represents an observer namespace, typically used for scoping handler streams.|
|»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|» failedPartitions|[[FailedObserverPartition](#schemafailedobserverpartition)]¦null|false|none|Gets or sets the failed partitions for the observer.|
|»» eventSourceId|[EventSourceId](#schemaeventsourceid)|false|none|Represents the unique identifier of an instance of an event source.|
|»»» value|string¦null|false|none|Gets or inits the underlying value for the instance.|
|»»» isSpecified|boolean|false|read-only|Check whether or not the Aksio.Cratis.Events.EventSourceId is specified.|
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

#### Enumerated Values

|Property|Value|
|---|---|
|type|0|
|type|1|
|type|2|
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

<h1 id="aksio-cratis-server-pii">PII</h1>

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
|body|body|[CreateAndRegisterKeyFor](#schemacreateandregisterkeyfor)|false|M:Aksio.Cratis.Compliance.Domain.GDPR.PII.CreateAndRegisterKeyFor(Aksio.Cratis.Compliance.Domain.GDPR.CreateAndRegisterKeyFor) payload.|

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
|body|body|[DeletePIIForPerson](#schemadeletepiiforperson)|false|M:Aksio.Cratis.Compliance.Domain.GDPR.PII.DeletePIIForPerson(Aksio.Cratis.Compliance.Domain.GDPR.DeletePIIForPerson) payload.|

<h3 id="post__api_compliance_gdpr_pii_delete-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|

<aside class="success">
This operation does not require authentication
</aside>

<h1 id="aksio-cratis-server-people">People</h1>

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

*Represents an implementation of Aksio.Cratis.Applications.Queries.IClientObservable.*

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

<h1 id="aksio-cratis-server-projections">Projections</h1>

## get__api_events_store_projections

`GET /api/events/store/projections`

*Gets all projections.*

<h3 id="get__api_events_store_projections-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|The Aksio.Cratis.Execution.MicroserviceId to get projections for.|

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

<h3 id="get__api_events_store_projections-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_projections-responseschema">Response Schema</h3>

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

## get__api_events_store_projections_{projectionId}_collections

`GET /api/events/store/projections/{projectionId}/collections`

*Get all collections for projection.*

<h3 id="get__api_events_store_projections_{projectionid}_collections-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|microserviceId|query|[MicroserviceId](#schemamicroserviceid)|false|The Aksio.Cratis.Execution.MicroserviceId to get projection collections for.|
|projectionId|path|[ProjectionId](#schemaprojectionid)|true|Id of projection to get for.|

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

<h3 id="get__api_events_store_projections_{projectionid}_collections-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|

<h3 id="get__api_events_store_projections_{projectionid}_collections-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[ProjectionCollection](#schemaprojectioncollection)]|false|none|none|
|» name|string¦null|false|none|none|
|» documentCount|integer(int32)|false|none|none|

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
      }
    }
  },
  "context": {
    "eventSourceId": {
      "value": "string",
      "isSpecified": true
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

Represents an event that has been appended to an event log.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|metadata|[EventMetadata](#schemaeventmetadata)|false|none|Represents the metadata related to an event.|
|context|[EventContext](#schemaeventcontext)|false|none|Represents the context in which an event exists - typically what it was appended with.|
|content|object¦null|false|none|none|
|» **additionalProperties**|[JsonNode](#schemajsonnode)|false|none|none|

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
|occurred|string(date-time)|false|none|none|
|validFrom|string(date-time)|false|none|none|
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

Represents the generation of an Aksio.Cratis.Events.EventType.

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
    }
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
  "isOutbox": true
}

```

Represents the unique identifier of an event sequence.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|value|string(uuid)|false|none|Gets or inits the underlying value for the instance.|
|isEventLog|boolean|false|read-only|Get whether or not this is the default log event sequence.|
|isOutbox|boolean|false|read-only|Get whether or not this is the default outbox event sequence.|

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
|isSpecified|boolean|false|read-only|Check whether or not the Aksio.Cratis.Events.EventSourceId is specified.|

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
  }
}

```

Represents the type of an event.

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|[EventTypeId](#schemaeventtypeid)|false|none|Represents the concept of the unique identifier of a type of event.|
|generation|[EventGeneration](#schemaeventgeneration)|false|none|Represents the generation of an Aksio.Cratis.Events.EventType.|

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

<h2 id="tocS_ObserverNamespace">ObserverNamespace</h2>
<!-- backwards compatibility -->
<a id="schemaobservernamespace"></a>
<a id="schema_ObserverNamespace"></a>
<a id="tocSobservernamespace"></a>
<a id="tocsobservernamespace"></a>

```json
{
  "value": "string"
}

```

Represents an observer namespace, typically used for scoping handler streams.

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
      }
    }
  ],
  "eventSequenceId": {
    "value": "a860a344-d7b2-406e-828e-8d442f23f344",
    "isEventLog": true,
    "isOutbox": true
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
  "currentNamespace": {
    "value": "string"
  },
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
  "isRecoveringAnyPartition": true
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
|currentNamespace|[ObserverNamespace](#schemaobservernamespace)|false|none|Represents an observer namespace, typically used for scoping handler streams.|
|failedPartitions|[[FailedObserverPartition](#schemafailedobserverpartition)]¦null|false|none|Gets or sets the failed partitions for the observer.|
|recoveringPartitions|[[RecoveringFailedObserverPartition](#schemarecoveringfailedobserverpartition)]¦null|false|none|Gets or sets the failed partitions for the observer.|
|hasFailedPartitions|boolean|false|read-only|Gets whether or not there are any failed partitions.|
|isRecoveringAnyPartition|boolean|false|read-only|Gets whether or not there are any partitions being recovered.|

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

<h2 id="tocS_Tenant">Tenant</h2>
<!-- backwards compatibility -->
<a id="schematenant"></a>
<a id="schema_Tenant"></a>
<a id="tocStenant"></a>
<a id="tocstenant"></a>

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

