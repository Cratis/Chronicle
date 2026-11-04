// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllStorageAuthorizationDiagnostics } from 'Features/Diagnostics';
import { describeStorageAuthorizationFailure } from './storageAuthorization';
import strings from 'Strings';
import './StorageAuthorizationNotice.css';

export interface IStorageAuthorizationNotice {

    /**
     * Optional class name for layout.
     */
    className?: string;
}

/**
 * Explains that the kernel's storage user was refused, when it was. Shows nothing while loading, when the
 * diagnostics cannot be read, or when no authorization failure is recorded.
 */
export const StorageAuthorizationNotice = ({ className }: IStorageAuthorizationNotice) => {
    const [result] = AllStorageAuthorizationDiagnostics.use();

    if (!result.isReady || !result.isSuccess || result.data.length === 0) return null;

    return (
        <div role='alert' className={`workbench-storage-authorization-notice ${className ?? ''}`.trim()}>
            <strong>{strings.components.storageAuthorization.title}</strong>
            <ul>
                {result.data.map(failure => (
                    <li key={`${failure.database}/${failure.collection}/${failure.operation}`}>
                        {describeStorageAuthorizationFailure(failure, strings.components.storageAuthorization.failure)}
                    </li>
                ))}
            </ul>
            <span>{strings.components.storageAuthorization.hint}</span>
        </div>
    );
};
