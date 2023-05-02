// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Defines the context for identity.
 */
export interface IIdentityContext<TDetails = {}> {
    /**
     * The application specific details for the identity.
     */
    details: TDetails;

    /**
     * Whether the details are set.
     */
    isSet: boolean;

    /**
     * Refreshes the identity context.
     */
    refresh(): void;
}
