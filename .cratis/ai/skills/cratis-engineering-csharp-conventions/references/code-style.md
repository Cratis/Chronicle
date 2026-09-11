<!-- cratis-ai-managed: skills/cratis-engineering-csharp-conventions/references/code-style.md -->
# C# code style

## Records

Use `record` for every immutable data structure — events, commands, read
models, concepts, DTOs. Records supply value equality, immutability, and concise
syntax. A `record class` with `init`-only properties is equivalent when the type
also needs methods.

```csharp
public record <EventName>(<ConceptType> <PropertyName>);

public record <ModelName>(<IdentityType> Id, <ConceptType> <PropertyName>);
```

## Primary constructors

Use primary constructors for all types. They remove the field-plus-constructor
ceremony.

```csharp
// Preferred
public class <ClassName>(<CollaboratorType> <collaborator>)
{
    public async Task <MethodName>(<ArgumentType> <argument>) =>
        await <collaborator>.<Method>(<argument>);
}
```

When a type genuinely needs field initialization logic and cannot use a primary
constructor, declare private fields with the `_camelCase` prefix.

## `var`

Always use `var` for a local variable. The right-hand side already names the
type.

```csharp
var <identifier> = <Factory>.New();
var <items> = <source>.Where(<predicate>).ToList();
```

## Expression-bodied members

Use expression-bodied form for simple members.

```csharp
public string <PropertyName> => $"{<First>} {<Second>}";
public void <MethodName>(string <argument>) => <collaborator>.<Method>(<argument>);
```

## Collections

Return read-only sequence types from public APIs. Never expose a mutable
collection type — a caller could mutate state its owner is responsible for.

```csharp
// Preferred
public IEnumerable<<ItemType>> <MethodName>() => <source>.ToList();
public IReadOnlyDictionary<<KeyType>, <ItemType>> <MethodName>() => <source>;

// Avoid on a public API
public List<<ItemType>> <MethodName>() => <source>;
public Dictionary<<KeyType>, <ItemType>> <MethodName>() => <source>;
```

## Nullable reference types

Embrace the type system — it is the first defense against null bugs. When an
annotation says a value cannot be null, trust it.

```csharp
// Use is null / is not null
if (<value> is null) throw new <DomainException>();
if (<value> is not null) <statement>;

// Do not add a defensive check the annotation already guarantees
public void <MethodName>(<NonNullableType> <argument>)
{
    <statement>;
}

// Add ! only where the compiler cannot see what you can prove
var <identifier> = <source>.FirstOrDefault(<predicate>)!;
```

Never write `== null` or `!= null`.

## Async

- Use `async`/`await`; return `Task` or `Task<T>`.
- Do not suffix a method with `Async` unless the suffix disambiguates an
  overload.
- Never use `.Result` or `.Wait()`.

```csharp
public async Task <MethodName>(<ArgumentType> <argument>) =>
    await <collaborator>.<Method>(<argument>);

public async Task<<ResultType>?> <FindMethod>(<IdentityType> id) => <expression>;
```

## Immutability

Prefer immutable designs. Produce a modified copy with a `with` expression
rather than mutating in place.

```csharp
var updated = <existing> with { <PropertyName> = <newValue> };
```

The owner of state is responsible for its mutations. Do not return a mutable
object a caller could change behind the owner's back.

## Pattern matching

Use pattern matching and switch expressions wherever they read better than a
branch chain.

```csharp
if (<result> is <ResultType>.Success success)
    return success.Value;

var <identifier> = <value> switch
{
    <EnumType>.<Member> => <expression>,
    <EnumType>.<OtherMember> => <expression>,
    _ => <fallback>
};
```

## String interpolation

```csharp
// Preferred
var message = $"<text> '{<value>}' <text>";

// Avoid
var message = string.Format("<text> '{0}' <text>", <value>);
var message = "<text> '" + <value> + "' <text>";
```

## Interface bodies

Omit the body of a member-less interface.

```csharp
// Preferred
public interface <IMarkerName>;

// Avoid
public interface <IMarkerName> { }
```

## XML documentation

XML documentation is a public API's first impression. Every public type, method,
property, and operator carries it.

- `<summary>` is **always multiline** — opening and closing tags on their own
  lines. Never cram it onto one line.
- Every method or operator with parameters includes a `<param name="…">` for
  each one.
- Every non-void method or operator includes `<returns>`.
- Every method that throws documents it with `<exception cref="…">`.
- Cross-reference with `<see cref="…"/>` and `<paramref name="…"/>`.
- Keep summaries concise. Document only where it adds understanding beyond the
  name.

```csharp
/// <summary>
/// Represents <description>.
/// </summary>
/// <param name="<parameterName>">The <see cref="<ParameterType>"/> to <purpose>.</param>
public class <ClassName>(<ParameterType> <parameterName>) : <IInterfaceName>
{
    /// <summary>
    /// <Verb> the <subject>.
    /// </summary>
    /// <param name="<argumentName>">The <see cref="<ArgumentType>"/> to <purpose>.</param>
    /// <returns>A <see cref="<ReturnType>"/> representing <description>.</returns>
    public async Task<<ReturnType>> <MethodName>(<ArgumentType> <argumentName>)
    {
        <statement>;
    }
}
```
