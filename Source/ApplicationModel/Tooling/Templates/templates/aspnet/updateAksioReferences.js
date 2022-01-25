try {
    const fs = require('fs');
    const https = require('https');
    const path = require('path');

    async function getFrom(host, path) {
        const options = {
            hostname: host,
            port: 443,
            path: path,
            method: 'GET'
        };

        return new Promise((resolve) => {
            const req = https.request(options, res => {
                let body = '';
                res.on('data', _ => {
                    body += _;
                });

                res.on('end', () => {
                    resolve(JSON.parse(body));
                });
            });

            req.end();
        });
    }

    async function handleNuGetPackages() {
        const file = path.join(__dirname, 'Directory.Build.props');

        console.log(`Working on file ${file}`);

        const content = fs.readFileSync(file).toString();
        const nugetPackages = [
            'Aksio.Defaults',
            'Aksio.Cratis.Applications',
            'Aksio.Cratis.Applications.CQRS',
            'Aksio.Cratis.Applications.CQRS.MongoDB',
            'Aksio.Cratis.Applications.ProxyGenerator'
        ];

        let result = content;

        for (const package of nugetPackages) {
            console.log(`Handle package ${package}`);

            const searchResult = await getFrom('azuresearch-usnc.nuget.org', `/query\?q\=${package}\&prerelease\=false\&semVerLevel\=2.0.0`);
            const version = searchResult.data[0].version;
            console.log(`Setting '${package}' to version '${version}' based on latest version on NuGet.`);

            const expression = `(?<=<PackageReference Include="${package}" Version=")[\\w\\.\\-]+(?=")`;
            const regex = new RegExp(expression, 'g');
            result = result.replace(regex, version);
        }
        fs.writeFileSync(file, result);
    }

    async function handleNpmPackages() {
        const file = path.join(__dirname, 'Web', 'package.json');
        if (!fs.existsSync(file)) return;

        console.log(`Working on file ${file}`);

        const content = fs.readFileSync(file).toString();
        const npmPackages = [
            '@aksio/frontend'
        ];

        let result = content;

        for (const package of npmPackages) {
            console.log(`Handle package ${package}`);

            const searchResult = await getFrom('registry.npmjs.org', `/${package}`);
            const version = searchResult['dist-tags'].latest;
            console.log(`Setting '${package}' to version '${version}' based on latest version on NPM.`);
            const expression = `(?<="${package}": ")[\\w\\.\\-]+(?=")`;
            const regex = new RegExp(expression, 'g');
            result = result.replace(regex, version);
        }

        fs.writeFileSync(file, result);
    }

    console.log('Updating Aksio NuGet and NPM package references.');
    (async () => await handleNuGetPackages())();
    (async () => await handleNpmPackages())();
} catch (ex) {
    console.error(ex);
    throw ex;
}
