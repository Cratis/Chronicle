import { Link } from '@fluentui/react';

export const Home = () => {
    return (
        <div style={{margin: '1rem'}}>
            <h1>Congratulations on your new microservice! 🍾 🎂 </h1>
            This microservice comes with a default Aksio setup.<br/>
            <br/>
            <h2>Projects</h2>
            <ul>
                <li>Concepts - used for holding reusable domain concepts.</li>
                <li>Domain - holds the business logic related to performing actions.</li>
                <li>Events - holds all the events for the microservice.</li>
                <li>Read - holds projections and read models.</li>
                <li>Main - the backend startup project.</li>
                <li>Web - this Web frontend.</li>
            </ul>
            <h2>Now what? Learn more 📖 🧠</h2>
            Get familiar with how to work with microservices and software in general @ Aksio.
            <ul>
                <li><Link href="https://github.com/aksio-system/Home/blob/main/README.md" target="__blank">Aksio Home on GitHub - describing our expectations and standards.</Link></li>
                <li><Link href="https://github.com/aksio-system" target="__blank">Writing automated specifications (BDD - Specification by Example).</Link></li>
                <li><Link href="https://github.com/aksio-system/Foundation/blob/main/Documentation/index.md" target="__blank">Aksio Foundation documentation.</Link></li>
                <li><Link href="https://github.com/aksio-system/Defaults" target="__blank">Get familiar with the defaults which is used for static code analysis.</Link></li>
            </ul>
            <h2>Debugging tools</h2>
            <ul>
                <li><Link href="/events" target="__blank">Event Workbench</Link></li>
                <li><Link href="/swagger" target="__blank">Swagger API</Link></li>
            </ul>
        </div>
    );
};