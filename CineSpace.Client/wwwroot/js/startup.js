// Start live data requests while WebAssembly downloads. Consume each result once;
// never persist sessions or catalogue responses in localStorage or the PWA cache.
(() => {
    const pending = new Map();
    const started = Date.now();
    function key(value) {
        const url = new URL(value, document.baseURI);
        if (url.origin !== location.origin) return null;
        url.searchParams.sort();
        return url.pathname + url.search;
    }
    function preload(path) {
        pending.set(key(path), fetch(path, { credentials: 'same-origin', cache: 'no-store', priority: 'low' })
            .then(async response => response.ok ? await response.text() : null).catch(() => null));
    }
    preload('/api/auth/sesion');
    preload('/api/catalogos/referencias');
    if (location.pathname === '/' || location.pathname === '/cine') {
        preload('/api/presentacion/banners');
        preload('/api/peliculas?pagina=1&tamano=8&orden=recientes&campo=Fecha&destacadas=False&minimo=0&maximo=0&estado=cartelera');
    }
    window.cineStartup = {
        async take(url) {
            const name = key(url), request = pending.get(name);
            pending.delete(name);
            // Expired data is fetched normally; a tab left loading must not reuse
            // old availability or a session from before a later login.
            return request && Date.now() - started < 20000 ? await request : null;
        },
        clear() { pending.clear(); }
    };
})();
