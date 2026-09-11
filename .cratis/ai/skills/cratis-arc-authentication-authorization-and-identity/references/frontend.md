<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/frontend.md -->
# Frontend identity

Verified against `@cratis/arc` and `@cratis/arc.react` `22.10.4`.

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
| `static readonly CookieName = '.cratis-identity'` | The cookie `/.cratis/me` writes |
| `static setHttpHeadersCallback(callback)` | Headers to send on the refresh call |
| `static setApiBasePath(path)` / `static setOrigin(origin)` | Where `/.cratis/me` lives |
| `static getCurrent<TDetails>(type?)` | Reads the **cookie**, no network call |
| `static refresh<TDetails>(type?)` | Fetches `<apiBasePath>/.cratis/me` |
| `static clearIdentityCookie()` | Expires the cookie |

`IIdentityProvider` is an abstract class with a single
`getCurrent<TDetails>(type?)`, which is what a view model injects.

`IdentityProviderResult` on the wire is
`{ id: string; name: string; roles: string[]; details: object }`.

`getCurrent` reads the cookie and never touches the network. That is what makes
it cheap, and also why nothing it returns is a security decision — the cookie is
deliberately not `HttpOnly`, so the browser can read *and write* it.

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

⚠️ **This hides UI; it does not protect data.** The identity comes from a cookie
the browser can edit, so anyone can render these children at will. Every query
and command behind the gate must carry its own `[Authorize]`/`[Roles]` on the
server, where the decision cannot be edited.

## MVVM

`@cratis/arc.react.mvvm` has **no** identity hook, provider, context, or type. It
registers `IIdentityProvider` → `IdentityProvider` in its container so a view
model can constructor-inject it. That is the whole integration.
