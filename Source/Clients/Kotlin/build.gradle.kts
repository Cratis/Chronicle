import com.google.protobuf.gradle.id

plugins {
    kotlin("jvm") version "2.1.0"
    id("com.google.protobuf") version "0.9.4"
    `java-library`
    id("com.vanniktech.maven.publish") version "0.30.0"
}

group = "io.cratis"
version = providers.gradleProperty("version").getOrElse("0.0.0-SNAPSHOT")

repositories {
    mavenCentral()
}

val grpcVersion = "1.70.0"
val grpcKotlinVersion = "1.4.1"
val protobufVersion = "4.29.3"
val coroutinesVersion = "1.9.0"

dependencies {
    api("io.grpc:grpc-kotlin-stub:$grpcKotlinVersion")
    api("io.grpc:grpc-protobuf:$grpcVersion")
    api("io.grpc:grpc-stub:$grpcVersion")
    api("com.google.protobuf:protobuf-kotlin:$protobufVersion")
    api("org.jetbrains.kotlinx:kotlinx-coroutines-core:$coroutinesVersion")
    compileOnly("javax.annotation:javax.annotation-api:1.3.2")
}

kotlin {
    jvmToolchain(17)
}

protobuf {
    protoc {
        artifact = "com.google.protobuf:protoc:$protobufVersion"
    }
    plugins {
        create("grpc") {
            artifact = "io.grpc:protoc-gen-grpc-java:$grpcVersion"
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
    publishToMavenCentral(com.vanniktech.maven.publish.SonatypeHost.CENTRAL_PORTAL)
    signAllPublications()

    coordinates("io.cratis", "chronicle-contracts", version.toString())

    pom {
        name.set("Chronicle Contracts")
        description.set("Generated gRPC Kotlin client for the Cratis Chronicle event sourcing platform")
        url.set("https://github.com/cratis/chronicle")
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
