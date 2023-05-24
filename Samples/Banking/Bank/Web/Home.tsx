// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Link } from '@fluentui/react';
import { default as styles } from './Home.module.scss';
import { useIdentity } from '@aksio/cratis-applications-frontend/identity';
import { Identity } from './Identity';

export const Home = () => {
    const identity = useIdentity<Identity>({
        firstName: 'Unknown',
        lastName: 'Unknown'
    });

    return (
        <div style={{ margin: '1rem' }} className={styles.home}>
            <h1>Congratulations on your new microservice! 🍾 🎂 </h1>
            This microservice comes with a default Aksio setup.<br />
            <br />
            <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
            <button onClick={() => identity.refresh()}>Refresh identity</button>
            <h2>Projects</h2>
            <ul>
                <li>Concepts - used for holding reusable domain concepts.</li>
                <li>Domain - holds the business logic related to performing actions.</li>
                <li>Events - holds all the events for the microservice.</li>
                <li>Events.Public - holds all the public events that you want to communicate outside of the microservice.</li>
                <li>Integration - holds artifacts used for integrating with non-event sourced systems.</li>
                <li>Read - holds projections and read models.</li>
                <li>Reactions - holds imperative observers that produces reactions typically other than state.</li>
                <li>Public - holds projections and possible other reactions that produces the public events to the outbox.</li>
                <li>Main - the backend startup project.</li>
                <li>Web - this Web frontend.</li>
            </ul>
            <h2>Now what? Learn more 📖 🧠</h2>
            Get familiar with how to work with microservices and software in general @ Aksio.
            <ul>
                <li><Link href="https://github.com/aksio-insurtech/Home/blob/main/README.md" target="__blank">Aksio Home on GitHub - describing our expectations and standards.</Link></li>
                <li><Link href="https://github.com/aksio-insurtech" target="__blank">Writing automated specifications (BDD - Specification by Example).</Link></li>
                <li><Link href="https://github.com/aksio-insurtech/Cratis/blob/main/Documentation/index.md" target="__blank">Aksio Cratis documentation.</Link></li>
                <li><Link href="https://github.com/aksio-insurtech/Defaults" target="__blank">Get familiar with the defaults which is used for static code analysis.</Link></li>
            </ul>
            <h2>Debugging tools</h2>
            <ul>
                <li><Link href="http://localhost:8080" target="__blank">Cratis Workbench</Link></li>
                <li><Link href="/swagger" target="__blank">Swagger API</Link></li>
            </ul>
        </div>
    );
};
