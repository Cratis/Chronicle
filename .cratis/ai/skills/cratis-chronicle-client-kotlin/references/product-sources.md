<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/product-sources.md -->
# Product sources

This reference holds the Chronicle.Kotlin artifacts and the source module each comes from.

| Artifact | Source |
| --- | --- |
| `io.cratis:chronicle` | `Chronicle.Kotlin` |
| `io.cratis:chronicle-spring-boot-starter` | `Integrations/SpringBoot` |
| `io.cratis:chronicle-testing` | `Testing` |
| `io.cratis:chronicle-contracts` | `Source/build.gradle.kts` (`chronicleContractsVersion`) |

All three artifacts are published to Maven Central under `io.cratis` (their
`build.gradle.kts` publishing blocks). The JVM toolchain is **17**.
