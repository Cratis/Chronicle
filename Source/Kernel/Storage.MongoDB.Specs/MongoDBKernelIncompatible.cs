// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// The exception that is thrown when a MongoDB spec container fails to start because the
/// host's Linux kernel falls in MongoDB's documented incompatible range.
/// </summary>
/// <param name="innerException">The underlying container-start failure this diagnoses.</param>
public class MongoDBKernelIncompatible(Exception innerException) : Exception(
    "The MongoDB spec container failed to start because mongod refused to run on this host's Linux kernel. " +
    "This is MongoDB's own startup guard, not a Chronicle defect: kernel 6.19 through 7.0.13 has a known incompatibility " +
    "between the kernel's rseq behavior and the TCMalloc allocator MongoDB 8.0+ vendors, so mongod exits immediately rather " +
    "than risk a crash or data corruption (https://jira.mongodb.org/browse/SERVER-121912). The fix lands on the kernel side, " +
    "not the MongoDB side: kernel 7.0.14+ or 7.1.0+ restores the previous behavior and mongod runs normally again, and there " +
    "is no supported flag to bypass this check (https://jira.mongodb.org/browse/SERVER-125742). On Docker Desktop for Mac, " +
    "the affected kernel is the LinuxKit VM's, not the host machine's - run 'docker info | grep -i kernel' to check it, and " +
    "'docker desktop update' to pick up a newer one once Docker Desktop ships kernel 7.0.14+. This is very unlikely to affect " +
    "CI: GitHub-hosted Ubuntu runners track a kernel well below 6.19. To unblock local verification before Docker Desktop " +
    "ships a fixed kernel, CHRONICLE_SPECS_MONGODB_IMAGE points these specs at a different image - but that changes what is " +
    "actually being verified, so treat it as a stopgap, not a fix.",
    innerException);
