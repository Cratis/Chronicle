# Identity

Based on the output of the [Aksio Middleware](https://github.com/aksio-insurtech/IngressMiddleware), the frontend benefits from
getting the user details provided by the application all the way into the frontend without having to make an extra call to the
backend.

While in development mode on your local machine, if there is no cookie it will call the `.aksio/me` endpoint from the frontend
itself. This makes it possible to work without having to simulate the production environment.

> Important note: Since local development is not configured with the identity provider, but you still need a way to test that both the backend and the frontend
> deals with the identity in the correct way. This can be achieved by creating the correct token and injecting it as request headers using
> a browser extension. Read more [here](../generating-principal.md).

This information is stored in a cookie called `.aksio-identity`. The content of this is a base64 encoded string containing the
JSON structure returned by the backend to the ingress middleware.

## Identity context

To use the identity system you need to provide the identity context for your application.

At the top level of your application, typically in your `App.tsx` file you would add the provider by doing the following:

```typescript
import { IdentityProvider } from '@aksio/cratis-applications-frontend/identity';

export const App = () => {
    return (
        <IdentityProvider>
            {/* ... your app content ... */}
        </IdentityProvider>
    );
};
```

This context can then be used anywhere by consuming the React context directly:

```typescript
import { IdentityProviderContext } from '@aksio/cratis-applications-frontend/identity';

export const SomeComponent = () => {
    return (
        <IdentityProviderContext.Consumer>
            {({ details }) => {
                const actualDetails = details as Identity;
                return (
                    <h1>{actualDetails.firstName} {actualDetails.firstName}</h1>
                );
            }}
        </IdentityProviderContext.Consumer>
    );
};
```

> Note: As you can see, the `details` type will be of type `any` in the context. This means that if your type is
> a specific type, you'll need to cast it to that type before using it.

### Refreshing

Sometimes you need to refresh the identity due to backend changes. On the `IIdentityContext` that represents
the context you find a method called `refresh()`. Calling this will invalidate the cookie and also just call
the backend to get the current identity details.

```typescript
import { IdentityProviderContext } from '@aksio/cratis-applications-frontend/identity';

export const SomeComponent = () => {
    return (
        <IdentityProviderContext.Consumer>
            {(identity) => {
                const actualDetails = identity.details as Identity;
                return (
                    <h1>{actualDetails.firstName} {actualDetails.firstName}</h1>

                    {/* Refresh button */}
                    <button onClick={() => identity.refresh()}>Refresh identity</button>
                );
            }}
        </IdentityProviderContext.Consumer>
    );
};
```

## useIdentity() hook

Anywhere within your application you can then access the identity by adding using the `useIdentity()` hook:

```typescript
import { useIdentity } from '@aksio/cratis-applications-frontend/identity';

export const Home = () => {
    const identity = useIdentity();

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

The `useIdentity()` hook returns the context which holds a property called `details`. This details property is what the backend
returned to the ingress middleware.

By default, if not specified, the type of the details is `any`. You can change this by passing it a generic argument with
the exact shape of what's expected:

```typescript
import { useIdentity } from '@aksio/cratis-applications-frontend/identity';

type Identity = {
    firstName: string;
    lastName: string;
};

export const Home = () => {
    const identity = useIdentity<Identity>();

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

### Refreshing with hook

Since the `useIdentity()` returns an instance of the `IIdentityContext`. So for refreshing with a hook, its easily
accessible:

```typescript
import { useIdentity } from '@aksio/cratis-applications-frontend/identity';

type Identity = {
    firstName: string;
    lastName: string;
};

export const Home = () => {
    const identity = useIdentity<Identity>();

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>

        {/* Refresh button */}
        <button onClick={() => identity.refresh()}>Refresh identity</button>
    );
};
```

## Default value

You can also provide a default value for the `details` property in the identity context.
If you don't provide one, it will default to an empty object, `{}`.
This is especially useful when working in local development and the cookie has not been provided

The default value can be provided as an argument to the `useIdentity()` hook:

```typescript
import { useIdentity } from '@aksio/cratis-applications-frontend/identity';

type Identity = {
    firstName: string;
    lastName: string;
};

export const Home = () => {
    const identity = useIdentity<Identity>({
        firstName: '[N/A]',
        lastName: '[N/A]'
    });

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```
