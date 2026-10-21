<!-- cratis-ai-managed: skills/cratis-arc-command-kotlin/references/authorization-override-example.md -->
# Authorization override example

This reference holds a worked example of an operation that overrides its class authorization.

```kotlin
@ReadModel
@Authorize
data class AuthenticationQueryItem(val message: String) {
    companion object {
        // Overrides the class: anyone may subscribe to this one.
        @JvmStatic
        @AllowAnonymous
        fun anonymous(@FromServices source: AuthenticationQuerySource): Flow<AuthenticationQueryItem> =
            source.observeAnonymous()
    }
}
```
