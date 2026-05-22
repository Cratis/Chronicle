---
applyTo: "**/*.ts,**/*.tsx"
---

# Code Quality — TypeScript

TypeScript/React-specific applications of the general [Code Quality](./code-quality.instructions.md) principles.

## Composition over Inheritance

React is built on composition — components accept children, hooks compose other hooks, and higher-order utilities wrap behavior. Avoid class hierarchies entirely; the language and framework have moved on.

```tsx
// ❌ Inheritance — fragile, couples component to base class internals
class SpecialButton extends BaseButton {
    override render() { ... }
}

// ✅ Composition — wrap or delegate, keep each piece independent
export const SpecialButton = ({ onClick, label }: SpecialButtonProps) => (
    <Button onClick={onClick} className="special">
        {label}
    </Button>
);
```

**Rules:**
- Never use class inheritance for React components — compose with props, children, and hooks instead.
- Extract repeated UI patterns into small, focused components rather than adding conditions to a shared parent.
- Extract repeated logic into custom hooks — a hook that does two unrelated things should be two hooks.

## Open/Closed Principle

TypeScript discriminated unions and generic constraints let you add new variants without touching existing code. Prefer them over ever-growing `if-else` / `switch` chains.

```ts
// ❌ Grows every time a new shape is needed
function area(shape: string, a: number, b?: number): number {
    if (shape === 'circle') return Math.PI * a * a;
    if (shape === 'rectangle') return a * (b ?? 0);
    throw new Error('Unknown shape');
}

// ✅ New shapes extend the union — existing handler functions are untouched
type Circle = { kind: 'circle'; radius: number };
type Rectangle = { kind: 'rectangle'; width: number; height: number };
type Shape = Circle | Rectangle;

function area(shape: Shape): number {
    switch (shape.kind) {
        case 'circle': return Math.PI * shape.radius ** 2;
        case 'rectangle': return shape.width * shape.height;
    }
}
```

**Rules:**
- Model variation with discriminated unions rather than optional fields or string literals.
- Design utility functions to accept an interface or generic constraint so new types can be handled by adding a new implementation, not by editing existing code.

## Separation of Concerns

React components have one job: render UI and delegate events. Keep data-fetching, business logic, and side effects in dedicated hooks or services — not inline in the component body.

**Rules:**
- Never write data-fetching or business logic directly in a component — extract it into a hook.
- Component files (`.tsx`) must not import from infrastructure layers such as HTTP clients or storage utilities directly — go through an abstraction or a generated proxy.
- Keep style concerns in co-located `.css` files; keep data concerns in hooks; keep rendering in the component.

## Low Coupling

Coupling in TypeScript is often hidden in deep import paths. Barrel files and path aliases make coupling explicit and keep refactoring safe.

**Rules:**
- Import from barrel `index.ts` files, not from deep internal paths — this limits the blast radius of refactoring.
- Use the configured path aliases (e.g. `Strings`, `Components`) rather than relative `../../../` chains.
- Never import from an unrelated feature's internal files — go through that feature's public barrel export.
- Keep the number of imports in a single file reasonable — many imports from many different areas is a coupling smell.

## Cross-Cutting Concerns

**Rules:**
- Use React Error Boundaries to centralize error display — never scatter `try/catch` blocks inside component render paths.
- Use a single top-level provider or hook for global state (e.g. authentication, theming) — never drill context down through many component layers.
- Centralize API error handling in a shared hook or service layer — do not duplicate toast/notification logic per component.
- Apply logging, analytics, and monitoring at the infrastructure edge (e.g. router callbacks, global error handlers) so that feature components remain unaware of them.
