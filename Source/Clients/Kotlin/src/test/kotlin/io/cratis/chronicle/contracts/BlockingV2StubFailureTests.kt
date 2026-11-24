// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package io.cratis.chronicle.contracts

import Cratis.Chronicle.Contracts.EventStores.EventStoresGrpc
import com.google.protobuf.Empty
import io.grpc.Status
import io.grpc.StatusRuntimeException
import io.grpc.inprocess.InProcessChannelBuilder
import io.grpc.inprocess.InProcessServerBuilder
import java.util.concurrent.TimeUnit
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFailsWith

class BlockingV2StubFailureTests {
    @Test
    fun `failed unary call throws a status runtime exception`() {
        val serverName = InProcessServerBuilder.generateName()
        val server = InProcessServerBuilder.forName(serverName)
            .directExecutor()
            .addService(object : EventStoresGrpc.EventStoresImplBase() {})
            .build()
            .start()
        val channel = InProcessChannelBuilder.forName(serverName).directExecutor().build()
        try {
            val stub = EventStoresGrpc.newBlockingV2Stub(channel).withDeadlineAfter(5, TimeUnit.SECONDS)
            val failure = assertFailsWith<StatusRuntimeException> {
                stub.allEventStores(Empty.getDefaultInstance())
            }
            assertEquals(Status.Code.UNIMPLEMENTED, failure.status.code)
        } finally {
            channel.shutdownNow()
            server.shutdownNow()
        }
    }
}
