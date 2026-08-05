// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { FailedPartition } from 'Api/Observation';
import { IDetailsComponentProps } from '@cratis/components/DataPage';
import { getFailedPartitionAttemptsNewestFirst } from './getFailedPartitionAttemptsNewestFirst';
import css from './FailedPartitionDetails.module.css';

export const FailedPartitionDetails = ({ item }: IDetailsComponentProps<FailedPartition>) => {
    const detailStrings = strings.eventStore.namespaces.failedPartitions.details;
    const attempts = getFailedPartitionAttemptsNewestFirst(item);

    const properties = [
        { label: detailStrings.observer, value: item.observerId },
        { label: detailStrings.partition, value: item.partition },
        { label: detailStrings.attempts, value: item.attempts.length.toString() }
    ];

    return (
        <div className={css.failedPartitionDetails}>
            <h2 className={css.title}>{item.partition}</h2>
            <dl className={css.properties}>
                {properties.map(property => (
                    <div key={property.label} className={css.property}>
                        <dt className={css.propertyLabel}>{property.label}</dt>
                        <dd className={css.propertyValue}>{property.value}</dd>
                    </div>
                ))}
            </dl>

            <h3 className={css.attemptsTitle}>{detailStrings.attempts}</h3>
            <div className={css.attempts}>
                {attempts.map((attempt, attemptIndex) => (
                    <div key={`${attempt.sequenceNumber}-${attemptIndex}`} className={css.attempt}>
                        <div className={css.attemptHeader}>
                            <span className={css.attemptOccurred}>{attempt.occurred.toLocaleString()}</span>
                            <span className={css.attemptSequenceNumber}>{`${detailStrings.sequenceNumber}: ${attempt.sequenceNumber}`}</span>
                        </div>
                        {attempt.messages.length > 0 && (
                            <>
                                <div className={css.sectionLabel}>{detailStrings.messages}</div>
                                <ul className={css.messages}>
                                    {attempt.messages.map((message, messageIndex) => (
                                        <li key={messageIndex} className={css.message}>{message}</li>
                                    ))}
                                </ul>
                            </>
                        )}
                        <div className={css.sectionLabel}>{detailStrings.stackTrace}</div>
                        {attempt.stackTrace
                            ? <pre className={css.stackTrace}>{attempt.stackTrace}</pre>
                            : <p className={css.noStackTrace}>{detailStrings.noStackTrace}</p>}
                    </div>
                ))}
            </div>
        </div>
    );
};
