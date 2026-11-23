import com.google.protobuf.gradle.id
import org.jetbrains.kotlin.gradle.dsl.KotlinVersion

plugins {
    kotlin("jvm") version "2.4.21"
    id("com.google.protobuf") version "0.10.1"
    `java-library`
    id("com.vanniktech.maven.publish") version "0.37.0"
}

group = "io.cratis"
version = providers.gradleProperty("version").getOrElse("0.0.0-SNAPSHOT")

repositories {
    mavenCentral()
}

val grpcVersion = "1.85.1"
// Preserve the published BlockingV2Stub unary exception contract while upgrading the runtime.
val grpcJavaGeneratorVersion = "1.70.0"
val grpcKotlinVersion = "1.5.0"
val protobufVersion = "4.36.2"
val coroutinesVersion = "1.11.0"

dependencies {
    api(kotlin("stdlib", "2.2.20"))
    api("io.grpc:grpc-kotlin-stub:$grpcKotlinVersion")
    api("io.grpc:grpc-protobuf:$grpcVersion")
    api("io.grpc:grpc-stub:$grpcVersion")
    api("com.google.protobuf:protobuf-kotlin:$protobufVersion")
    api("org.jetbrains.kotlinx:kotlinx-coroutines-core:$coroutinesVersion")
    compileOnly("javax.annotation:javax.annotation-api:1.3.2")
    testImplementation(kotlin("test"))
    testImplementation("io.grpc:grpc-inprocess:$grpcVersion")
}

kotlin {
    jvmToolchain(17)
    compilerOptions {
        apiVersion.set(KotlinVersion.KOTLIN_2_2)
        languageVersion.set(KotlinVersion.KOTLIN_2_2)
        allWarningsAsErrors.set(true)
    }
}

tasks.test {
    useJUnitPlatform()
}

// The canonical descriptor set travels with the package so a client can hand the kernel exactly the contracts it
// was built against, and the kernel can own the one compatibility check instead of every language reimplementing
// it. See Source/Kernel/Compatibility.
val embedDescriptorSet = tasks.register<Copy>("embedDescriptorSet") {
    from(file("../../Kernel/Protobuf/chronicle.desc"))
    into(layout.buildDirectory.dir("generated/resources/chronicle"))
}

sourceSets.named("main") {
    resources.srcDir(layout.buildDirectory.dir("generated/resources"))
}

tasks.named("processResources") {
    dependsOn(embedDescriptorSet)
}

// sourcesJar (created by the maven-publish plugin) packages sourceSets.main.allSource, which reads the
// generated resources directory directly rather than through processResources - so it needs the same
// explicit dependency to avoid Gradle's implicit-dependency validation failure.
tasks.matching { it.name == "sourcesJar" }.configureEach {
    dependsOn(embedDescriptorSet)
}

protobuf {
    protoc {
        artifact = "com.google.protobuf:protoc:$protobufVersion"
    }
    plugins {
        create("grpc") {
            artifact = "io.grpc:protoc-gen-grpc-java:$grpcJavaGeneratorVersion"
        }
        create("grpckt") {
            artifact = "io.grpc:protoc-gen-grpc-kotlin:$grpcKotlinVersion:jdk8@jar"
        }
    }
    generateProtoTasks {
        all().forEach { task ->
            task.plugins {
                create("grpc")
                create("grpckt")
            }
            task.builtins {
                create("kotlin")
            }
        }
    }
}

mavenPublishing {
    publishToMavenCentral()
    signAllPublications()

    coordinates("io.cratis", "chronicle-contracts", version.toString())

    pom {
        name.set("Cratis Chronicle gRPC Contracts for Kotlin and Java")
        description.set("Generated gRPC contracts for Cratis Chronicle, the open-source event-sourcing database and runtime.")
        url.set("https://www.cratis.io/chronicle/")
        licenses {
            license {
                name.set("MIT License")
                url.set("https://opensource.org/licenses/MIT")
                distribution.set("repo")
            }
        }
        developers {
            developer {
                id.set("cratis")
                name.set("Cratis")
                email.set("post@cratis.io")
            }
        }
        scm {
            url.set("https://github.com/cratis/chronicle")
            connection.set("scm:git:git://github.com/cratis/chronicle.git")
            developerConnection.set("scm:git:ssh://git@github.com/cratis/chronicle.git")
        }
    }
}

// The sources jar (registered by signAllPublications() above, but only once the maven-publish plugin's own
// afterEvaluate callback runs) packages the main source set's resources, which now include the generated
// directory embedDescriptorSet writes into - without this, Gradle's implicit-dependency validation fails the
// build because nothing orders the two tasks relative to each other. afterEvaluate defers this until after
// that callback has had a chance to register the task.
afterEvaluate {
    tasks.named("sourcesJar") {
        dependsOn(embedDescriptorSet)
    }
}
