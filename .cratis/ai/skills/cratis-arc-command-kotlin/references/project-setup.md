<!-- cratis-ai-managed: skills/cratis-arc-command-kotlin/references/project-setup.md -->
# Project setup

This reference holds the Gradle settings and build files for an Arc.Kotlin project.

```kotlin
// settings.gradle.kts
pluginManagement {
    repositories {
        mavenCentral()
        gradlePluginPortal()
    }
}
```

```kotlin
// build.gradle.kts
plugins {
    id("io.cratis.arc") version "<version>"
    kotlin("plugin.spring") version "2.4.20"
    id("org.springframework.boot") version "4.1.1"
    id("io.spring.dependency-management") version "1.1.7"
}

repositories {
    mavenCentral()
}

cratisArc {
    moduleName.set("<ModuleName>")
    dependencyVersion.set("<version>")
    endpoints {
        segmentsToSkip.set(<packageSegmentsUnderModuleRoot>)
    }
}

dependencies {
    implementation("io.cratis:arc-spring-boot-starter:<version>")
    implementation("org.springframework.boot:spring-boot-starter-webmvc")
}
```
