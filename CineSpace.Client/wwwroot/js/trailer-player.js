import { openAuthDialog, closeAuthDialog } from './auth-dialog.js';

export function connectTrailer(dialog, reference) {
    if (!(dialog instanceof HTMLDialogElement) || !dialog.isConnected) return { dispose() {} };
    const stage = dialog.querySelector('.trailer-stage');
    const full = dialog.querySelector('[data-trailer-fullscreen]');
    let disposed = false;
    const id = openAuthDialog(dialog, reference);
    const toggle = async () => {
        try {
            if (document.fullscreenElement) await document.exitFullscreen();
            else if (stage.requestFullscreen) await stage.requestFullscreen();
        } catch { /* The embedded player retains its own fullscreen control. */ }
    };
    const doubleClick = event => { if (!event.target.closest('button')) toggle(); };
    const changed = () => {
        const active = !!document.fullscreenElement;
        full.setAttribute('aria-label', active ? 'Salir de pantalla completa' : 'Pantalla completa');
        full.title = active ? 'Salir de pantalla completa' : 'Pantalla completa';
        full.querySelector('i').className = active ? 'fas fa-compress' : 'fas fa-expand';
    };
    full.addEventListener('click', toggle);
    stage.addEventListener('dblclick', doubleClick);
    document.addEventListener('fullscreenchange', changed);
    return {
        async dispose() {
            if (disposed) return;
            disposed = true;
            full.removeEventListener('click', toggle);
            stage.removeEventListener('dblclick', doubleClick);
            document.removeEventListener('fullscreenchange', changed);
            if (document.fullscreenElement === stage) await document.exitFullscreen().catch(() => {});
            closeAuthDialog(id);
        }
    };
}
