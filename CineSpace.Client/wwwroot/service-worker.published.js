self.importScripts('./service-worker-assets.js');

const cachePrefix = 'cinespace-shell-';
const cacheName = `${cachePrefix}${self.assetsManifest.version}`;
const baseUrl = new URL(self.registration.scope);
// Solo la interfaz. No almacenar datos de cuentas, compras ni respuestas de API.
const shellAsset = /^(?:_framework\/|css\/|fonts\/|js\/|vendor\/fontawesome\/)|^(?:index\.html|appsettings\.json|manifest\.webmanifest|CineSpace\.Client\.styles\.css|favicon\.png|android-chrome-(?:192x192|512x512)\.png)$/;
const assets = self.assetsManifest.assets.filter(asset => shellAsset.test(asset.url));
const assetUrls = new Set(assets.map(asset => new URL(asset.url, baseUrl).href));

self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const cache = await caches.open(cacheName);
        await cache.addAll(assets.map(asset => new Request(new URL(asset.url, baseUrl), {
            integrity: asset.hash, cache: 'no-cache', credentials: 'same-origin'
        })));
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        await Promise.all(keys.filter(key => key.startsWith(cachePrefix) && key !== cacheName)
            .map(key => caches.delete(key)));
    })());
});

self.addEventListener('fetch', event => {
    const request = event.request;
    const url = new URL(request.url);
    // OAuth también es navegación; nunca debe recibir el index.html del caché.
    if (request.method !== 'GET' || url.origin !== baseUrl.origin ||
        /^\/(?:api(?:\/|$)|signin-google(?:\/|$)|healthz(?:\/|$))/i.test(url.pathname)) return;

    if (request.mode === 'navigate') {
        event.respondWith((async () => {
            const cache = await caches.open(cacheName);
            return await cache.match(new URL('index.html', baseUrl).href) || fetch(request);
        })());
    } else if (assetUrls.has(url.href)) {
        event.respondWith((async () => {
            const cache = await caches.open(cacheName);
            return await cache.match(request) || fetch(request);
        })());
    }
});
