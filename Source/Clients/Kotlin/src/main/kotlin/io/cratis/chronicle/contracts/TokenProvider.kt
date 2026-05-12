// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.chronicle.contracts

import java.net.URI
import java.net.URLEncoder
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.nio.charset.StandardCharsets
import java.time.Instant

interface TokenProvider {
    fun getAccessToken(): String?

    fun refresh(): String?
}

class NoOpTokenProvider : TokenProvider {
    override fun getAccessToken(): String? = null

    override fun refresh(): String? = null
}

class OAuthTokenProvider(
    private val tokenEndpoint: URI,
    private val clientId: String,
    private val clientSecret: String,
    private val httpClient: HttpClient = HttpClient.newBuilder().build(),
) : TokenProvider {
    @Volatile
    private var accessToken: String? = null

    @Volatile
    private var tokenExpiry: Instant = Instant.EPOCH

    @Synchronized
    override fun getAccessToken(): String? {
        if (!accessToken.isNullOrBlank() && Instant.now().isBefore(tokenExpiry)) {
            return accessToken
        }

        return fetchAccessToken()
    }

    @Synchronized
    override fun refresh(): String? {
        accessToken = null
        tokenExpiry = Instant.EPOCH
        return fetchAccessToken()
    }

    private fun fetchAccessToken(): String {
        val requestBody = buildString {
            append("grant_type=client_credentials")
            append("&client_id=")
            append(urlEncode(clientId))
            append("&client_secret=")
            append(urlEncode(clientSecret))
        }

        val request = HttpRequest.newBuilder(tokenEndpoint)
            .header("Content-Type", "application/x-www-form-urlencoded")
            .POST(HttpRequest.BodyPublishers.ofString(requestBody))
            .build()

        val response = httpClient.send(request, HttpResponse.BodyHandlers.ofString())
        if (response.statusCode() != 200) {
            throw IllegalStateException("Token request failed with status ${response.statusCode()}: ${response.body()}")
        }

        val body = response.body()
        val token = Regex("\"access_token\"\\s*:\\s*\"([^\"]+)\"")
            .find(body)
            ?.groupValues
            ?.get(1)
            ?.takeIf { it.isNotBlank() }
            ?: throw IllegalStateException("Token response did not include access_token")

        val expiresIn = Regex("\"expires_in\"\\s*:\\s*(\\d+)")
            .find(body)
            ?.groupValues
            ?.get(1)
            ?.toLongOrNull()
            ?: 3600L

        accessToken = token
        tokenExpiry = Instant.now().plusSeconds((expiresIn - 60).coerceAtLeast(1))
        return token
    }

    private fun urlEncode(value: String) = URLEncoder.encode(value, StandardCharsets.UTF_8)
}