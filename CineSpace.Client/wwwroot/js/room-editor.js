const editors = new WeakMap();
function alignScreenLabel(grid) {
    const view = grid.closest('.room-grid-scroll'), screen = grid.parentElement?.querySelector('.room-screen'), label = screen?.querySelector('span');
    if (!view || !screen || !label) return;
    const viewport = view.getBoundingClientRect(), bar = screen.getBoundingClientRect();
    const left = Math.max(viewport.left + 8, bar.left), right = Math.min(viewport.left + view.clientWidth - 8, bar.right);
    label.style.left = `${(left + right - label.offsetWidth) / 2 - bar.left}px`;
}
export function connectRoomEditor(grid, reference) {
    const abort = new AbortController(); const options = {signal: abort.signal};
    let painting = null, anchor = null, suppressClick = false, queue = Promise.resolve();
    const enabled = () => grid.dataset.disabled !== 'true';
    const cell = target => target instanceof Element ? target.closest('button[data-seat]') : null;
    const inside = button => button && grid.contains(button);
    const focus = button => { grid.querySelectorAll('button[data-seat][tabindex="0"]').forEach(b => b.tabIndex = -1); button.tabIndex = 0; button.focus({preventScroll: true}); };
    const preview = (button, value) => {
        if ((button.getAttribute('aria-pressed') === 'true') === value) return;
        button.classList.toggle('is-seat', value); button.classList.toggle('is-gap', !value);
        button.setAttribute('aria-pressed', String(value)); button.setAttribute('aria-label', `${button.dataset.seat}, ${value ? 'asiento' : 'pasillo'}`);
        button.title = `${button.dataset.seat} · ${value ? 'Asiento' : 'Pasillo'}`;
        painting.ids.add(button.dataset.seat);
    };
    const commit = () => {
        if (!painting) return;
        const {ids, value} = painting; painting = null;
        if (ids.size) queue = queue.then(() => reference.invokeMethodAsync('AplicarTrazo', [...ids], value)).catch(() => {});
    };
    const begin = (button, rectangle) => {
        painting = {ids: new Set(), value: button.getAttribute('aria-pressed') !== 'true'};
        if (rectangle && anchor?.isConnected) {
            painting.value = anchor.getAttribute('aria-pressed') === 'true';
            const top = Math.min(+anchor.dataset.row, +button.dataset.row), bottom = Math.max(+anchor.dataset.row, +button.dataset.row);
            const left = Math.min(+anchor.dataset.col, +button.dataset.col), right = Math.max(+anchor.dataset.col, +button.dataset.col);
            for (const position of grid.querySelectorAll('button[data-seat]')) {
                const r = +position.dataset.row, c = +position.dataset.col;
                if (r >= top && r <= bottom && c >= left && c <= right) preview(position, painting.value);
            }
        } else { preview(button, painting.value); anchor = button; }
        focus(button);
    };
    grid.addEventListener('pointerdown', event => {
        if (!enabled() || event.pointerType === 'touch' || event.button !== 0) return;
        const button = cell(event.target); if (!inside(button)) return;
        event.preventDefault(); begin(button, event.shiftKey); suppressClick = true;
        grid.setPointerCapture(event.pointerId);
    }, options);
    grid.addEventListener('pointermove', event => {
        if (!painting) return;
        const button = cell(document.elementFromPoint(event.clientX, event.clientY));
        if (inside(button)) preview(button, painting.value);
    }, options);
    const end = event => { if (!painting) return; commit(); if (grid.hasPointerCapture(event.pointerId)) grid.releasePointerCapture(event.pointerId); setTimeout(() => { suppressClick = false; }, 0); };
    grid.addEventListener('pointerup', end, options); grid.addEventListener('pointercancel', end, options);
    grid.addEventListener('click', event => {
        if (suppressClick) { suppressClick = false; return; }
        const button = cell(event.target); if (!enabled() || !inside(button)) return;
        begin(button, event.shiftKey); commit();
    }, options);
    grid.addEventListener('keydown', event => {
        if (!enabled()) return;
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'z') { event.preventDefault(); queue = queue.then(() => reference.invokeMethodAsync('Historia', event.shiftKey)).catch(() => {}); return; }
        const axis = event.target.closest?.('button[data-axis-col],button[data-axis-row]');
        if (axis && ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(event.key)) {
            event.preventDefault(); let next;
            if (axis.hasAttribute('data-axis-col')) {
                const c = +axis.dataset.axisCol;
                next = event.key === 'ArrowDown' ? grid.querySelector(`[data-row="0"][data-col="${c}"]`) : grid.querySelector(`[data-axis-col="${c + (event.key === 'ArrowRight' ? 1 : -1)}"]`);
            } else {
                const r = +axis.dataset.axisRow;
                next = event.key === 'ArrowRight' ? grid.querySelector(`[data-row="${r}"][data-col="0"]`) : grid.querySelector(`[data-axis-row="${r + (event.key === 'ArrowDown' ? 1 : -1)}"]`);
            }
            if (next) { if (next.hasAttribute('data-seat')) focus(next); else next.focus({preventScroll: true}); next.scrollIntoView({block: 'nearest', inline: 'nearest'}); } return;
        }
        const button = cell(event.target); if (!inside(button)) return;
        if (event.key === ' ' || event.key === 'Enter') { event.preventDefault(); begin(button, event.shiftKey); commit(); return; }
        let r = +button.dataset.row, c = +button.dataset.col;
        if (event.key === 'ArrowUp' && r === 0 || event.key === 'ArrowLeft' && c === 0) {
            event.preventDefault(); grid.querySelector(event.key === 'ArrowUp' ? `[data-axis-col="${c}"]` : `[data-axis-row="${r}"]`)?.focus({preventScroll: true}); return;
        }
        if (event.key === 'ArrowUp') r--; else if (event.key === 'ArrowDown') r++; else if (event.key === 'ArrowLeft') c--; else if (event.key === 'ArrowRight') c++;
        else if (event.key === 'Home') { c = 0; if (event.ctrlKey) r = 0; }
        else if (event.key === 'End') { c = +grid.dataset.cols - 1; if (event.ctrlKey) r = +grid.dataset.rows - 1; }
        else return;
        event.preventDefault(); r = Math.max(0, Math.min(+grid.dataset.rows - 1, r)); c = Math.max(0, Math.min(+grid.dataset.cols - 1, c));
        const next = grid.querySelector(`[data-row="${r}"][data-col="${c}"]`);
        if (next) { focus(next); next.scrollIntoView({block: 'nearest', inline: 'nearest'}); }
    }, options);
    const view = grid.closest('.room-grid-scroll');
    let frame;
    const updateScreen = () => { cancelAnimationFrame(frame); frame = requestAnimationFrame(() => alignScreenLabel(grid)); };
    const resize = new ResizeObserver(updateScreen); resize.observe(grid); if (view) { resize.observe(view); view.addEventListener('scroll', updateScreen, options); }
    updateScreen();
    editors.set(grid, {abort, resize, stop: () => cancelAnimationFrame(frame)});
}
export function disconnectRoomEditor(grid) { const state = editors.get(grid); state?.abort.abort(); state?.resize.disconnect(); state?.stop(); editors.delete(grid); }
export function refreshRoomEditor(grid) {
    alignScreenLabel(grid);
    const active = document.activeElement;
    if (!grid.contains(active) || !active.matches('button[data-seat]')) return;
    grid.querySelectorAll('button[data-seat][tabindex="0"]').forEach(b => b.tabIndex = -1);
    active.tabIndex = 0;
}

