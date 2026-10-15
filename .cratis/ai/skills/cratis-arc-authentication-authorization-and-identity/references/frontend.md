<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/frontend.md -->
# Frontend identity

## Contents

- `@cratis/arc/identity`
- `@cratis/arc.react/identity`
- `RequireRole`
- MVVM

Verified against `@cratis/arc` and `@cratis/arc.react` `22.75.0`.

## `@cratis/arc/identity`

```ts
interface IIdentity<TDetails = object> {
    id: string;
    name: string;
    roles: string[];
    details: TDetails;
    isSet: boolean;
    isInRole(role: string): boolean;
    refresh(): Promise<IIdentity<TDetails>>;
}
```

`IdentityProvider` is the framework-neutral implementation:

| Member | Notes |
| --- | --- |
| `static readonly CookieName = '.cratis-identity'` | Deprecated legacy cookie name; current backend neither reads nor writes identity there |
| `static setHttpHeadersCallback(callback)` | Headers to send on the refresh call |
| `static setApiBasePath(path)` / `static setOrigin(origin)` | Where `/.cratis/me` lives |
| `static getCurrent<TDetails>(type?)` | Fetches `/.cratis/me`, caches a resolved identity in memory |
| `static refresh<TDetails>(type?)` | Fetches `<apiBasePath>/.cratis/me` |
| `static clearCache()` | Forgets memory cache and expires a legacy cookie; use on logout |
| `static clearIdentityCookie()` | Deprecated alias for `clearCache()` |

`IIdentityProvider` is an abstract class with a single
`getCurrent<TDetails>(type?)`, which is what a view model injects.

`IdentityProviderResult` on the wire is
`{ id: string; name: string; roles: string[]; details: object }`.

`getCurrent` fetches once for a resolved identity; `refresh` always asks again.
Only a 404 enables the warned legacy-cookie fallback; a cookie never overrides
401/403. Stop reading `.cratis-identity` in application code. Neither the memory
cache nor the legacy fallback is an authorization boundary.
Source: [Arc v22.75.0 IdentityProvider.ts](https://github.com/Cratis/Arc/blob/v22.75.0/Source/JavaScript/Arc/identity/IdentityProvider.ts).

## `@cratis/arc.react/identity`

```ts
interface IIdentityContext<TDetails = object> extends IIdentity<TDetails> {
    isLoading: boolean;
    clearIdentity: () => void;
}
```

`IdentityProvider` is the React component. Props: `children`,
`httpHeadersCallback`, and `detailsType` (a constructor, for type-safe details).
It seeds `isLoading: true` and fetches on mount. The root `<Arc>` component takes
the same `detailsType` prop and renders this provider for you.

When a refresh finds that an identity which was set is now unset (the server-side
session ended), the React `IdentityProvider` reloads the page with a full
top-level navigation rather than leaving the tab signed out and stuck. Do not rely
on a signed-out render surviving a refresh.

`useIdentity` has two overloads:

```ts
useIdentity<TDetails>(type: Constructor<TDetails>, defaultDetails?): IIdentityContext<TDetails>
useIdentity<TDetails>(defaultDetails?): IIdentityContext<TDetails>
```

The default details stand in **whenever there are no details to give**, not only
when the identity is explicitly unset — an identity that resolved while carrying
no details still has `isSet` true, and without the stand-in the first property
access on the details throws and takes the page down.

`isLoading` distinguishes "the identity has not arrived yet" from "the caller is
anonymous". Treating the two alike makes a signed-in user flash the forbidden
state on every load.

## `RequireRole`

```tsx
<RequireRole roles={[<role>]} whileLoading={<spinner/>} forbidden={<denied/>}>
    {children}
</RequireRole>
```

Props are `RequireRoleSlots` (`children`, `whileLoading?`, `forbidden?`) crossed
with either `{ roles: string[]; allow? }` or `{ roles?; allow }`, where `allow` is
`(details: TDetails | undefined, identity: IIdentityContext<TDetails>) => boolean`.
When both are supplied, **both** must pass.

Everything that is not an unambiguous yes renders `forbidden`:

- neither `roles` nor `allow` supplied (it warns and denies — that is what a
  renamed configuration key looks like);
- `roles` supplied but not an array;
- the identity is not set;
- no role matches, or the predicate does not return exactly `true`;
- the predicate throws;
- an `allow` predicate with absent or `null` details.

⚠️ **This hides UI; it does not protect data.** Identity comes from the endpoint,
but anyone can edit browser state and render these children at will. Every query
and command behind the gate must carry its own `[Authorize]`/`[Roles]` on the
server, where the decision cannot be edited.

## MVVM

`@cratis/arc.react.mvvm` has **no** identity hook, provider, context, or type. It
registers `IIdentityProvider` → `IdentityProvider` in its container so a view
model can constructor-inject it. That is the whole integration.
