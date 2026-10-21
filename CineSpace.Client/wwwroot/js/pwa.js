// Las actualizaciones esperan a que se cierren las pestañas: no interrumpen una compra.
if ('serviceWorker' in navigator && window.isSecureContext) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/service-worker.js', { updateViaCache: 'none' })
            .catch(() => console.warn('No se pudo preparar la instalación de CineSpace.'));
    }, { once: true });
}
