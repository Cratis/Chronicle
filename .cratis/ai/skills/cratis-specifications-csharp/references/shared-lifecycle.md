<!-- cratis-ai-managed: skills/cratis-specifications-csharp/references/shared-lifecycle.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Optional shared xUnit lifecycle

Source: `Cratis.Specifications.XUnit`,
`SpecificationTestFramework.cs`, `SpecificationTestClassRunner.cs`, and
`Specification.cs`.

Requires `Cratis.Specifications.XUnit` 4.1.0.

The house default remains stock xUnit
per-fact execution.

## Opt in

Add this assembly attribute to the specification project:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[assembly: Xunit.TestFramework("Cratis.Specifications.SpecificationTestFramework", "Cratis.Specifications.XUnit")]
```

For eligible classes, construction, `Establish`, and `Because` happen once.
All facts share the instance; `Destroy` runs once after the last fact, including
when initialization failed. A throwing `Establish` or `Because` fails every fact
in that class. Facts must only assert, never mutate shared context.

A class is eligible only when it derives from `Specification`, has a single
public parameterless constructor, does not implement `IDisposable`, and has only
plain facts. Theories, custom fact test cases, fixture/output-helper constructors,
and plain xUnit classes keep the stock per-fact lifecycle. This is not xUnit v3
support; the package uses xUnit 2.x.

`Specification.InitializeAsync()` is idempotent for a single instance. It does
not make an ineligible class shared or cache instances across classes.
