<!-- cratis-ai-managed: skills/cratis-arc-command-validation/references/input-binding.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Command input binding and member paths

## Explicit offsets

Requires Arc 22.110.0. Check with `dotnet list package`; older Arc has no
`[ExplicitOffset]` binding contract. Require an explicit offset at the input
boundary instead of accepting a server-local interpretation.

Use `[property: ExplicitOffset]` on a `DateTimeOffset` or `DateTime` command
property, nullable or not. It refuses date-time text without an offset or `Z`
with `DateTime command property '<member>' requires an explicit offset or Z.`
`DateTimeOffset` retains the input offset; `DateTime` becomes UTC. The attribute
is in `Cratis.Arc.Commands`.

Requires Arc 22.120.0 for concepts over `DateTime` or `DateTimeOffset` with
Arc's default JSON options. Concepts over `DateOnly` are refused as a
configuration error: calendar dates have no offset. The marker affects
JSON-bound properties, including nested collection elements, not query strings
or route scalars. Use reflection-based JSON metadata for these properties;
System.Text.Json source-generated metadata does not support derived
`JsonConverterAttribute` markers.

## Malformed bodies still authorize

Requires Arc 22.110.0 for authorization-before-binding-failure handling on
execute and `/validate`. An unauthorized caller gets 403 even if the body is
malformed; an authorized caller gets a `malformedRequest` validation result.
Its message is generic, and its member is a normalized field path such as
`from` or `items[2].when`, not parser or CLR type details.

A resource-dependent policy cannot run without a bound command; those policies
still produce a binding validation failure for an unbindable body. Requirements
that never read the resource may implement
`Cratis.Arc.Authorization.IResourceFreeRequirement` so their
policies can run; built-in role, claim, name and authenticated-user requirements
are already resource-free. Do not mark a requirement resource-free if it reads
the command.

## Annotations on concepts

Requires Arc 22.121.2 for data annotations on command properties and model-bound
query parameters to validate a concept's wrapped value: `[Required]` rejects an
empty wrapped string, `[Range]` checks the primitive, and `[StringLength]` does
not throw on the wrapper. This uses generated validation metadata; types without
that metadata (including external assemblies) keep the previous behavior. Do
not assume annotations replace a concept's reusable validator.

## Validation members are field paths

Requires Arc 22.115.0 for **all** `ValidationResult.Members` paths to be
camelCase, including data annotations and errors returned by `Provide()` or
`Handle()`. Use `name`, `address.street` and `items[0].name` in server assertions,
not `nameof(Property)` or PascalCase paths. Authors may still construct a result
with `[nameof(Property)]`; Arc normalizes it before clients receive it. Older
Arc can fail to bind such results to the matching form field.

## Verify

Add specs for offset-less rejection, an accepted offset, malformed input under
both authorized and denied callers, and nested/indexed member paths. Assert
server `Members` using camelCase and run the owning spec project's built executable (see [specs.csharp](../../../rules/specs.csharp.md)).
For form behavior, assert the rejection appears on its intended field, not only
in a command-wide error list.
