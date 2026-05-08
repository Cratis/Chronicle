// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.chronicle.contracts

import io.grpc.ChannelCredentials
import io.grpc.InsecureChannelCredentials
import io.grpc.TlsChannelCredentials
import java.net.URI
import java.net.URLDecoder
import java.net.URLEncoder
import java.nio.charset.StandardCharsets

enum class AuthenticationMode {
    ClientCredentials,
    ApiKey,
}

data class ChronicleServerAddress(
    val host: String,
    val port: Int,
)

class ChronicleConnectionString(connectionString: String) {
    companion object {
        const val DEVELOPMENT_CLIENT = "chronicle-dev-client"
        const val DEVELOPMENT_CLIENT_SECRET = "chronicle-dev-secret"
        private const val DEFAULT_PORT = 35000

        @JvmField
        val Default = ChronicleConnectionString("chronicle://localhost:$DEFAULT_PORT")

        @JvmField
        val Development = ChronicleConnectionString(
            "chronicle://$DEVELOPMENT_CLIENT:$DEVELOPMENT_CLIENT_SECRET@localhost:$DEFAULT_PORT",
        )
    }

    val serverAddress: ChronicleServerAddress
    val username: String?
    val password: String?
    val apiKey: String?
    val disableTls: Boolean
    val certificatePath: String?
    val certificatePassword: String?

    private val scheme: String
    private val queryParameters: Map<String, String>

    val authenticationMode: AuthenticationMode
        get() {
            val hasClientCredentials = !username.isNullOrBlank() && !password.isNullOrBlank()
            val hasApiKey = !apiKey.isNullOrBlank()

            if (hasClientCredentials && hasApiKey) {
                throw IllegalStateException("Cannot specify both client credentials and API key authentication")
            }

            if (hasClientCredentials) {
                return AuthenticationMode.ClientCredentials
            }

            if (hasApiKey) {
                return AuthenticationMode.ApiKey
            }

            throw IllegalStateException("No authentication method specified. Provide either client credentials or API key")
        }

    init {
        val parsed = try {
            URI(connectionString)
        } catch (exception: Exception) {
            throw IllegalArgumentException("Invalid Chronicle connection string: ${exception.message}", exception)
        }

        scheme = parsed.scheme ?: throw IllegalArgumentException("Connection string must include a scheme")
        if (scheme != "chronicle" && scheme != "chronicle+srv") {
            throw IllegalArgumentException("Unsupported Chronicle scheme '$scheme'")
        }

        val host = parsed.host ?: throw IllegalArgumentException("Connection string must include a host")
        val port = if (parsed.port == -1) DEFAULT_PORT else parsed.port
        if (port !in 1..65535) {
            throw IllegalArgumentException("Connection string port must be between 1 and 65535")
        }
        serverAddress = ChronicleServerAddress(host, port)

        val userInfo = parsed.userInfo
        val userInfoParts = userInfo?.split(':', limit = 2).orEmpty()
        username = userInfoParts.getOrNull(0)?.takeIf { it.isNotBlank() }?.let { decode(it) }
        password = userInfoParts.getOrNull(1)?.takeIf { it.isNotBlank() }?.let { decode(it) }

        queryParameters = parseQuery(parsed.rawQuery)
        apiKey = queryParameters["apiKey"]
        disableTls = queryParameters["disableTls"]?.equals("true", ignoreCase = true) == true
        certificatePath = queryParameters["certificatePath"]
        certificatePassword = queryParameters["certificatePassword"]
    }

    fun withCredentials(username: String, password: String): ChronicleConnectionString {
        val updatedQuery = queryParameters.toMutableMap()
        updatedQuery.remove("apiKey")
        return create(username = username, password = password, query = updatedQuery)
    }

    fun withApiKey(apiKey: String): ChronicleConnectionString {
        val updatedQuery = queryParameters.toMutableMap()
        updatedQuery["apiKey"] = apiKey
        return create(username = null, password = null, query = updatedQuery)
    }

    fun createCredentials(): ChannelCredentials =
        if (disableTls) InsecureChannelCredentials.create() else TlsChannelCredentials.create()

    override fun toString(): String = createConnectionString(username, password, queryParameters)

    private fun create(username: String?, password: String?, query: Map<String, String>): ChronicleConnectionString =
        ChronicleConnectionString(createConnectionString(username, password, query))

    private fun createConnectionString(
        username: String?,
        password: String?,
        query: Map<String, String>,
    ): String {
        val authority = buildString {
            if (!username.isNullOrBlank()) {
                append(encode(username))
                if (!password.isNullOrBlank()) {
                    append(':')
                    append(encode(password))
                }
                append('@')
            }
            append(serverAddress.host)
            append(':')
            append(serverAddress.port)
        }

        val queryString = query
            .entries
            .sortedBy { it.key }
            .joinToString("&") { "${encode(it.key)}=${encode(it.value)}" }
            .takeIf { it.isNotBlank() }

        return buildString {
            append(scheme)
            append("://")
            append(authority)
            if (!queryString.isNullOrBlank()) {
                append('?')
                append(queryString)
            }
        }
    }

    private fun parseQuery(rawQuery: String?): Map<String, String> {
        if (rawQuery.isNullOrBlank()) {
            return emptyMap()
        }

        return rawQuery
            .split('&')
            .filter { it.isNotBlank() }
            .associate {
                val split = it.split('=', limit = 2)
                val key = decode(split[0])
                val value = decode(split.getOrElse(1) { "" })
                key to value
            }
    }

    private fun encode(value: String) = URLEncoder.encode(value, StandardCharsets.UTF_8)

    private fun decode(value: String) = URLDecoder.decode(value, StandardCharsets.UTF_8)
}