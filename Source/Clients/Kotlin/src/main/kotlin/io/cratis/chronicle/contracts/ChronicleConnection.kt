// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.chronicle.contracts

import Cratis.Chronicle.Contracts.EventStoresGrpcKt
import Cratis.Chronicle.Contracts.NamespacesGrpcKt
import Cratis.Chronicle.Contracts.Clients.ConnectionServiceGrpcKt
import Cratis.Chronicle.Contracts.EventSequences.EventSequencesGrpcKt
import Cratis.Chronicle.Contracts.Events.Constraints.ConstraintsGrpcKt
import Cratis.Chronicle.Contracts.Events.EventTypesGrpcKt
import Cratis.Chronicle.Contracts.Host.ServerGrpcKt
import Cratis.Chronicle.Contracts.Identities.IdentitiesGrpcKt
import Cratis.Chronicle.Contracts.Jobs.JobsGrpcKt
import Cratis.Chronicle.Contracts.Observation.FailedPartitionsGrpcKt
import Cratis.Chronicle.Contracts.Observation.ObserversGrpcKt
import Cratis.Chronicle.Contracts.Observation.Reactors.ReactorsGrpcKt
import Cratis.Chronicle.Contracts.Observation.Reducers.ReducersGrpcKt
import Cratis.Chronicle.Contracts.Projections.ProjectionsGrpcKt
import Cratis.Chronicle.Contracts.ReadModels.ReadModelsGrpcKt
import Cratis.Chronicle.Contracts.Recommendations.RecommendationsGrpcKt
import Cratis.Chronicle.Contracts.Seeding.EventSeedingGrpcKt
import io.grpc.CallOptions
import io.grpc.Channel
import io.grpc.ClientInterceptor
import io.grpc.ClientInterceptors
import io.grpc.ConnectivityState
import io.grpc.ManagedChannel
import io.grpc.ManagedChannelBuilder
import io.grpc.Metadata
import io.grpc.MethodDescriptor
import io.grpc.Status
import io.grpc.kotlin.AbstractCoroutineStub
import java.net.URI
import java.time.Duration
import java.time.Instant
import java.util.concurrent.TimeUnit
import kotlin.coroutines.resume
import kotlinx.coroutines.suspendCancellableCoroutine

data class ChronicleConnectionOptions(
    val connectionString: ChronicleConnectionString? = null,
    val serverAddress: String? = null,
    val connectTimeout: Duration = Duration.ofSeconds(10),
    val maxReceiveMessageSize: Int? = null,
    val maxSendMessageSize: Int? = null,
    val authority: String? = null,
    val managementPort: Int = 8080,
)

data class ChronicleServices(
    val eventStores: EventStoresGrpcKt.EventStoresCoroutineStub,
    val namespaces: NamespacesGrpcKt.NamespacesCoroutineStub,
    val recommendations: RecommendationsGrpcKt.RecommendationsCoroutineStub,
    val identities: IdentitiesGrpcKt.IdentitiesCoroutineStub,
    val eventSequences: EventSequencesGrpcKt.EventSequencesCoroutineStub,
    val eventTypes: EventTypesGrpcKt.EventTypesCoroutineStub,
    val constraints: ConstraintsGrpcKt.ConstraintsCoroutineStub,
    val observers: ObserversGrpcKt.ObserversCoroutineStub,
    val failedPartitions: FailedPartitionsGrpcKt.FailedPartitionsCoroutineStub,
    val reactors: ReactorsGrpcKt.ReactorsCoroutineStub,
    val reducers: ReducersGrpcKt.ReducersCoroutineStub,
    val projections: ProjectionsGrpcKt.ProjectionsCoroutineStub,
    val readModels: ReadModelsGrpcKt.ReadModelsCoroutineStub,
    val jobs: JobsGrpcKt.JobsCoroutineStub,
    val eventSeeding: EventSeedingGrpcKt.EventSeedingCoroutineStub,
    val server: ServerGrpcKt.ServerCoroutineStub,
)

class ChronicleConnection(options: ChronicleConnectionOptions = ChronicleConnectionOptions()) : AutoCloseable {
    val connectionString: ChronicleConnectionString

    val services: ChronicleServices

    val connectionService: ConnectionServiceGrpcKt.ConnectionServiceCoroutineStub

    @Volatile
    var isConnected: Boolean = false
        private set

    private val channel: ManagedChannel
    private val authTokenProvider: TokenProvider

    init {
        connectionString = when {
            options.connectionString != null -> options.connectionString
            !options.serverAddress.isNullOrBlank() -> ChronicleConnectionString("chronicle://${options.serverAddress}")
            else -> ChronicleConnectionString.Default
        }

        authTokenProvider = createTokenProvider(options)

        val builder = ManagedChannelBuilder
            .forAddress(connectionString.serverAddress.host, connectionString.serverAddress.port)

        if (connectionString.disableTls) {
            builder.usePlaintext()
        } else {
            builder.useTransportSecurity()
        }

        options.maxReceiveMessageSize?.let { builder.maxInboundMessageSize(it) }
        options.maxSendMessageSize?.let { builder.maxInboundMetadataSize(it) }

        channel = builder.build()

        val interceptedChannel = createAuthenticatedChannel(channel)
        connectionService = ConnectionServiceGrpcKt.ConnectionServiceCoroutineStub(interceptedChannel)
        services = ChronicleServices(
            eventStores = EventStoresGrpcKt.EventStoresCoroutineStub(interceptedChannel),
            namespaces = NamespacesGrpcKt.NamespacesCoroutineStub(interceptedChannel),
            recommendations = RecommendationsGrpcKt.RecommendationsCoroutineStub(interceptedChannel),
            identities = IdentitiesGrpcKt.IdentitiesCoroutineStub(interceptedChannel),
            eventSequences = EventSequencesGrpcKt.EventSequencesCoroutineStub(interceptedChannel),
            eventTypes = EventTypesGrpcKt.EventTypesCoroutineStub(interceptedChannel),
            constraints = ConstraintsGrpcKt.ConstraintsCoroutineStub(interceptedChannel),
            observers = ObserversGrpcKt.ObserversCoroutineStub(interceptedChannel),
            failedPartitions = FailedPartitionsGrpcKt.FailedPartitionsCoroutineStub(interceptedChannel),
            reactors = ReactorsGrpcKt.ReactorsCoroutineStub(interceptedChannel),
            reducers = ReducersGrpcKt.ReducersCoroutineStub(interceptedChannel),
            projections = ProjectionsGrpcKt.ProjectionsCoroutineStub(interceptedChannel),
            readModels = ReadModelsGrpcKt.ReadModelsCoroutineStub(interceptedChannel),
            jobs = JobsGrpcKt.JobsCoroutineStub(interceptedChannel),
            eventSeeding = EventSeedingGrpcKt.EventSeedingCoroutineStub(interceptedChannel),
            server = ServerGrpcKt.ServerCoroutineStub(interceptedChannel),
        )
    }

    suspend fun connect(timeout: Duration = Duration.ofSeconds(10)) {
        if (isConnected) {
            return
        }

        val deadline = Instant.now().plus(timeout)
        var state = channel.getState(true)

        while (Instant.now().isBefore(deadline)) {
            if (state == ConnectivityState.READY) {
                isConnected = true
                return
            }

            suspendCancellableCoroutine { continuation ->
                channel.notifyWhenStateChanged(state) {
                    if (continuation.isActive) {
                        continuation.resume(Unit)
                    }
                }
            }

            state = channel.getState(false)
        }

        isConnected = false
        throw IllegalStateException("Connection timed out before reaching READY state. Last state: $state")
    }

    fun disconnect(gracePeriod: Duration = Duration.ofSeconds(2)) {
        isConnected = false
        channel.shutdown()
        channel.awaitTermination(gracePeriod.toMillis(), TimeUnit.MILLISECONDS)
    }

    override fun close() {
        disconnect()
    }

    private fun createAuthenticatedChannel(baseChannel: Channel): Channel {
        val interceptor = authenticationInterceptor() ?: return baseChannel
        return ClientInterceptors.intercept(baseChannel, interceptor)
    }

    private fun authenticationInterceptor(): ClientInterceptor? {
        val apiKey = connectionString.apiKey
        val hasClientCredentials = try {
            connectionString.authenticationMode == AuthenticationMode.ClientCredentials
        } catch (_: Exception) {
            false
        }

        if (!hasClientCredentials && apiKey.isNullOrBlank()) {
            return null
        }

        val authorizationKey = Metadata.Key.of("authorization", Metadata.ASCII_STRING_MARSHALLER)
        val apiKeyKey = Metadata.Key.of("api-key", Metadata.ASCII_STRING_MARSHALLER)

        return object : ClientInterceptor {
            override fun <ReqT : Any?, RespT : Any?> interceptCall(
                method: MethodDescriptor<ReqT, RespT>,
                callOptions: CallOptions,
                next: io.grpc.Channel,
            ): io.grpc.ClientCall<ReqT, RespT> {
                val delegate = next.newCall(method, callOptions)
                return object : io.grpc.ForwardingClientCall.SimpleForwardingClientCall<ReqT, RespT>(delegate) {
                    override fun start(responseListener: Listener<RespT>, headers: Metadata) {
                        val token = try {
                            authTokenProvider.getAccessToken()
                        } catch (_: Exception) {
                            null
                        }

                        when {
                            !token.isNullOrBlank() -> headers.put(authorizationKey, "Bearer $token")
                            !apiKey.isNullOrBlank() -> headers.put(apiKeyKey, apiKey)
                        }

                        super.start(responseListener, headers)
                    }
                }
            }
        }
    }

    private fun createTokenProvider(options: ChronicleConnectionOptions): TokenProvider {
        val authMode = try {
            connectionString.authenticationMode
        } catch (_: Exception) {
            return NoOpTokenProvider()
        }

        if (authMode != AuthenticationMode.ClientCredentials) {
            return NoOpTokenProvider()
        }

        val clientId = connectionString.username ?: return NoOpTokenProvider()
        val clientSecret = connectionString.password ?: return NoOpTokenProvider()

        val tokenEndpoint = resolveTokenEndpoint(options)
        return OAuthTokenProvider(tokenEndpoint, clientId, clientSecret)
    }

    private fun resolveTokenEndpoint(options: ChronicleConnectionOptions): URI {
        val authorityUri = options.authority?.let { URI(it) }
        val host = authorityUri?.host ?: connectionString.serverAddress.host
        val port = if (authorityUri != null && authorityUri.port != -1) authorityUri.port else options.managementPort
        val scheme = when {
            authorityUri?.scheme != null -> authorityUri.scheme
            connectionString.disableTls -> "http"
            else -> "https"
        }
        return URI("$scheme://$host:$port/connect/token")
    }
}