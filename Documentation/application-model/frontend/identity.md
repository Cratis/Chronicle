# Identity

Based on the output of the [Aksio Middleware](https://github.com/aksio-insurtech/IngressMiddleware), the frontend benefits from
getting the user details provided by the application all the way into the frontend without having to make an extra call to the
backend.

This information is stored in a cookie called `.aksio-identity`. The content of this is a base64 encoded string containing the
JSON structure returned by the backend to the ingress middleware.

To use this, the Cratis application model provides a React context and a hook to make this more convenient to use.

At the top level of your application, typically in your `App.tsx` file you would add the provider by doing the following:

```tsx
import { IdentityProvider } from '@aksio/cratis-applications-frontend/identity';

export const App = () => {
    return (
        <IdentityProvider>
            {/* ... your app content ... */}
        </IdentityProvider>
    );
};
```

Anywhere within your application you can then access the identity by adding using the `useIdentity()` hook:

```tsx
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

```tsx
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
