// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// The exception that is thrown when a MongoDB spec container fails to start because the
/// host's Linux kernel falls in MongoDB's documented incompatible range, despite the standard
/// mitigation already being applied.
/// </summary>
/// <param name="innerException">The underlying container-start failure this diagnoses.</param>
public class MongoDBKernelIncompatible(Exception innerException) : Exception(
    "The MongoDB spec container failed to start because mongod refused to run on this host's Linux kernel, even " +
    "with the standard mitigation applied. This is MongoDB's own startup guard, not a Chronicle defect: kernel 6.19 " +
    "through 7.0.13 has a known incompatibility between the kernel's rseq behavior and the TCMalloc allocator " +
    "MongoDB 8.0.5+ vendors by default (https://jira.mongodb.org/browse/SERVER-121912). Both MongoDB spec fixtures " +
    "already set GLIBC_TUNABLES=glibc.pthread.rseq=1 - the documented fix that reverts to the pre-8.0.5 behavior " +
    "where glibc, not TCMalloc, owns rseq registration (https://github.com/docker-library/mongo/discussions/748) - " +
    "so seeing this exception means either the container was started outside that path, or " +
    "CHRONICLE_SPECS_MONGODB_IMAGE points at an image that does not honor the variable the same way the official " +
    "mongo image does. Check WithMongoDBKernelCompatibility() is applied, or verify the chosen image's own handling " +
    "of GLIBC_TUNABLES before falling back to a kernel upgrade (7.0.14+ or 7.1.0+ restores the previous kernel " +
    "behavior and needs no mitigation at all - https://jira.mongodb.org/browse/SERVER-125742).",
    innerException);
