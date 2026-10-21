import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';

const raw = process.env.RENDER_API_ORIGIN?.trim();
let origin;
try {
    const url = new URL(raw);
    if (url.protocol !== 'https:' || url.username || url.password || url.pathname !== '/' ||
        url.search || url.hash || url.hostname === 'localhost' ||
        !/^[a-z\d.-]+$/i.test(url.hostname)) throw new Error();
    origin = url.origin;
} catch {
    console.error('Configura RENDER_API_ORIGIN con la URL HTTPS pública de Render, sin rutas ni credenciales.');
    process.exit(1);
}

if (process.argv[2] !== '--validate') {
    const directory = resolve(process.argv[2] || '.publish/netlify/wwwroot');
    if (!existsSync(resolve(directory, 'service-worker-assets.js'))) {
        throw new Error('Falta publicar el cliente PWA en Release antes de preparar Netlify.');
    }
    // No modificar archivos de la PWA después de publicar: sus hashes son inmutables.
    const settings = JSON.parse(readFileSync(resolve(directory, 'appsettings.json'), 'utf8'));
    if (settings.Api?.BaseUrl || settings.Api?.HttpBaseUrl) {
        throw new Error('En Netlify el cliente debe utilizar el proxy del mismo origen (Api:BaseUrl vacío).');
    }
    writeFileSync(resolve(directory, '_redirects'), [
        `/api ${origin}/api 200!`,
        `/api/* ${origin}/api/:splat 200!`,
        `/signin-google ${origin}/signin-google 200!`,
        `/healthz ${origin}/healthz 200!`,
        '/* /index.html 200',
        ''
    ].join('\n'));
    console.log('Frontend PWA preparado con proxy hacia Render.');
}
