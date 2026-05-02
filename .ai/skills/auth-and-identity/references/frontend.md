# Frontend Identity

This reference covers consuming identity in React, MVVM, and vanilla TypeScript.

## How the Frontend Gets Identity

The frontend uses a **cookie-first** approach:

1. Check for the `.cratis-identity` cookie (base64-encoded JSON, `HttpOnly=false`)
2. If cookie exists → decode and use it (no HTTP call needed)
3. If no cookie → call `GET /.cratis/me` to fetch identity and set the cookie
4. In development mode, the cookie fallback (`/.cratis/me` call) always works — no ingress simulation needed

## React

### IdentityProvider Context

Wrap your app root with `<IdentityProvider>`:

```tsx
import { IdentityProvider } from '@cratis/arc.react/identity';

export const App = () => (
    <IdentityProvider>
        {/* your app */}
    </IdentityProvider>
);
```

For type-safe details with complex types (e.g., `Guid`), pass a `detailsType` constructor:

```tsx
import { IdentityProvider } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';

class UserIdentityDetails {
    userId: Guid = Guid.empty;
    firstName: string = '';
    lastName: string = '';
}

export const App = () => (
    <IdentityProvider detailsType={UserIdentityDetails}>
        {/* your app */}
    </IdentityProvider>
);
```

### `useIdentity()` Hook

Access identity anywhere in your component tree:

```tsx
import { useIdentity } from '@cratis/arc.react/identity';

type UserDetails = {
    department: string;
    title: string;
};

export const UserProfile = () => {
    const identity = useIdentity<UserDetails>();

    return (
        <div>
            <h3>{identity.name}</h3>
            <p>Department: {identity.details.department}</p>
        </div>
    );
};
```

**With default values** (useful for local development when the cookie might not exist):

```tsx
const identity = useIdentity<UserDetails>({
    department: '[N/A]',
    title: '[N/A]'
});
```

**With a constructor for type-safe deserialization** (uses `JsonSerializer.deserializeFromInstance()` under the hood):

```tsx
const identity = useIdentity(UserIdentityDetails);

// With default values:
const identity = useIdentity(UserIdentityDetails, {
    userId: Guid.empty,
    firstName: '[N/A]',
    lastName: '[N/A]'
});
```

### Role Checking

```tsx
const identity = useIdentity();

if (identity.isInRole('Admin')) {
    // show admin UI
}

// Or access roles directly
console.log(identity.roles);
```

### Refreshing Identity

When identity changes on the backend (e.g., user granted new roles):

```tsx
const identity = useIdentity();
const handleRefresh = () => identity.refresh();
```

This calls `GET /.cratis/me` again and updates both the cookie and context.

### `IIdentity<TDetails>` Shape

| Property | Type | Description |
|----------|------|-------------|
| `id` | `string` | Unique ID from identity provider |
| `name` | `string` | Display name |
| `roles` | `string[]` | Assigned roles |
| `details` | `TDetails` | Application-specific details |
| `isSet` | `boolean` | Whether identity has been loaded |
| `isInRole(role)` | `(string) => boolean` | Check role membership |
| `refresh()` | `() => Promise<IIdentity>` | Re-fetch identity from backend |

## MVVM (tsyringe)

In an MVVM setup, `IIdentityProvider` is automatically registered in the DI container by `Bindings.initialize()`:

```typescript
import { injectable } from 'tsyringe';
import { IIdentityProvider } from '@cratis/arc/identity';

type UserDetails = {
    department: string;
};

@injectable()
export class MyViewModel {
    constructor(private readonly _identityProvider: IIdentityProvider) {}

    async loadUser() {
        const identity = await this._identityProvider.getCurrent<UserDetails>();
        console.log(identity.details.department);
    }
}
```

Requires the [MVVM Context](Documentation/frontend/react.mvvm/mvvm-context.md) to be set up.

## Vanilla TypeScript (No Framework)

Use `IdentityProvider` directly:

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

const identity = await IdentityProvider.getCurrent<UserDetails>();
console.log(identity.name);
console.log(identity.isInRole('Admin'));
```

## Frontend Role-Based UI Pattern

Combine `useIdentity()` with authorization attributes on the backend for defense in depth:

```tsx
const identity = useIdentity();

return (
    <div>
        {identity.isInRole('Admin') && <AdminPanel />}
        {identity.isInRole('Manager') && <ManagerDashboard />}
        <PublicContent />
    </div>
);
```

The frontend check is for UX (hiding buttons the user can't use). The backend `[Roles]` attribute is the actual security boundary.
