<!-- cratis-ai-managed: skills/cratis-fundamentals-concept/references/typescript-concepts.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# TypeScript value concepts

## Contents

- Declare a concept
- Convert non-primitive values
- Serialization boundaries
- Other typed values

Verified against `@cratis/fundamentals` 7.23.0: `ConceptAs.ts`,
`JsonSerializer.ts`, and `Documentation/typescript/concept_as.md` in Fundamentals.

## Declare a concept

The convenient union pattern (7.13.0+) accepts either a concept instance or its
underlying primitive. Supply the runtime class, not the union type, to `@field`:

```typescript
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConceptAs, field, JsonSerializer } from '@cratis/fundamentals';

class UserIdConcept extends ConceptAs<string> {}
export type UserId = UserIdConcept | string;

class User {
    @field(UserIdConcept)
    id!: UserId;
}

const user = new User();
user.id = 'user-123';
const json = JsonSerializer.serialize(user);
const restored = JsonSerializer.deserialize(User, json);
```

`json` is `{"id":"user-123"}` and `restored.id` is a `UserIdConcept` instance.
The union is an assignment convenience, not nominal type safety: different
string-backed concepts still accept the same string. When callers must use
instances, export the class directly instead, as the Arc TypeScript validation
skill does with `Name`. `static readonly valueType = String` is valid but optional
for a string-backed concept; it does not require a different naming pattern.

## Convert non-primitive values

From 7.19.5, `valueType` tells deserialization to convert the inner payload before
constructing the concept. Without it, a Guid wire value remains a string:

```typescript
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConceptAs, field, Guid } from '@cratis/fundamentals';

class UserIdConcept extends ConceptAs<Guid> {
    static readonly valueType = Guid;
}
export type UserId = UserIdConcept | Guid;

class User {
    @field(UserIdConcept)
    id!: UserId;
}
```

## Serialization boundaries

Use `JsonSerializer.serialize(instance)` and
`JsonSerializer.deserialize(Type, json)` for typed conversion. Concepts serialize
to their inner value and deserialize to instances when their runtime class is
supplied as metadata. Collections and maps unwrap concepts too (7.17.0+).

Keep one resolved copy of `@cratis/fundamentals`: duplicate copies trigger a
warning and can cause identity/`instanceof` problems. The 7.23.0 serializer uses
stable type keys for concepts and converters, but that does not make all
cross-copy identity checks safe.

## Other typed values

The root exports `Guid`, `DateOnly`, `TimeOnly`, `TimeSpan`, `ValueMap`, `field`,
and `JsonSerializer`. Subpaths `/json`, `/geospatial`, and `/reflection` are
exported too. The package declarations support NodeNext resolution (7.19.6+).
Use `JsonSerializer.registerConverter(converter)` for a custom `JsonConverter`
instance, rather than replacing the serializer.

`@field` supports legacy and standard decorators (7.18.0+); standard fields need
decorator metadata support. Load `@cratis/fundamentals/reflection` before defining
metadata-dependent types. For arrays, declare the element constructor with
`@field(Array, { genericArguments: [Item] })`, where `Item` is the model's runtime
class, not a type alias (7.19.5+).

Dependent Arc/Chronicle peer ranges are not feature guarantees: check the actual
resolved Fundamentals version before relying on these version-qualified APIs.
