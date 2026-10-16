<!-- cratis-ai-managed: skills/cratis-chronicle-client-typescript/references/project-setup.md -->
# Project setup

This reference holds installation, the entry-point import, and decorator compiler settings.

## Project setup

```shell
yarn add @cratis/chronicle @cratis/fundamentals reflect-metadata
```

`@cratis/fundamentals` is a peer dependency, so it does not arrive on its own.
`reflect-metadata` is now a regular dependency. Import it **once, at the entry
point**, before any decorated class is loaded:

```typescript
import 'reflect-metadata';
```

The package is **ESM-only** and needs Node.js **22.19+** for its `undici`
dependency (the client itself has no `engines` field). Standard TC39 decorators
are supported (TypeScript 5.2+) and are the documentation default:

```jsonc
// tsconfig.json — standard decorator mode
{
    "compilerOptions": {
        "target": "ES2022",
        "lib": ["ES2022", "ESNext.Decorators"]
    }
}
```

Legacy mode with `experimentalDecorators: true` and
`emitDecoratorMetadata: true` still works. Standard decorators and esbuild/tsx
emit no type metadata: declare every serialized property with `@field(Type)`
from `@cratis/fundamentals`; default values are only an inference fallback.
