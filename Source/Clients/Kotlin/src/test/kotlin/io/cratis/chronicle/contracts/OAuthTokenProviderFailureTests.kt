// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.chronicle.contracts

import com.sun.net.httpserver.HttpServer
import java.net.InetSocketAddress
import java.net.URI
import kotlin.test.Test
import kotlin.test.assertContains
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class OAuthTokenProviderFailureTests {
    private val secret = "s3cr3t-client-value"
    private val marker = "leak-marker"

    private fun failureFor(status: Int, body: String): Throwable {
        val server = HttpServer.create(InetSocketAddress("127.0.0.1", 0), 0)
        server.createContext("/token") { exchange ->
            val bytes = body.toByteArray()
            exchange.sendResponseHeaders(status, bytes.size.toLong())
            exchange.responseBody.use { it.write(bytes) }
        }
        server.start()
        try {
            val provider = OAuthTokenProvider(URI("http://127.0.0.1:${server.address.port}/token"), "my-client", secret)
            try {
                provider.getAccessToken()
            } catch (e: Throwable) {
                return e
            }
            throw AssertionError("Expected the token request to fail")
        } finally {
            server.stop(0)
        }
    }

    private fun everyText(failure: Throwable): List<String> =
        generateSequence(failure) { it.cause }.flatMap { sequenceOf(it.message ?: "", it.toString()) }.toList()

    private fun assertNoLeak(failure: Throwable) {
        for (text in everyText(failure)) {
            assertFalse(text.contains(marker), "leaked marker: $text")
            assertFalse(text.contains(secret), "leaked secret: $text")
        }
    }

    @Test
    fun `response echoing the request does not leak the body`() {
        val body = """{"error":"invalid_client","error_description":"$marker client_secret=$secret"}"""
        val failure = failureFor(401, body)
        assertNoLeak(failure)
        assertContains(failure.message!!, "401")
        assertContains(failure.message!!, "invalid_client")
    }

    @Test
    fun `unparseable body echoing the request does not leak`() {
        val failure = failureFor(400, "grant_type=client_credentials&client_secret=$secret $marker")
        assertNoLeak(failure)
        assertContains(failure.message!!, "400")
        assertContains(failure.message!!, OAuthErrorCodes.NO_ERROR_CODE)
    }

    @Test
    fun `unknown error code maps to the generic value`() {
        val failure = failureFor(400, """{"error":"$marker-$secret"}""")
        assertNoLeak(failure)
        assertContains(failure.message!!, OAuthErrorCodes.NO_ERROR_CODE)
    }

    @Test
    fun `success without access_token does not leak the body`() {
        val failure = failureFor(200, """{"note":"$marker client_secret=$secret"}""")
        assertNoLeak(failure)
        assertTrue(failure.message!!.contains("access_token"))
    }
}
