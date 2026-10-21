const maps = new WeakMap();
export function connectSeatMap(element, summary, reference) {
    let initial = true, summaryReached, disconnected = false;
    const updateShortcut = () => {
        if (disconnected) return;
        const summaryEdge = window.innerHeight - Math.round(window.innerHeight * .45);
        const reached = summary.getClientRects().length > 0 && summary.getBoundingClientRect().top <= summaryEdge;
        if (summaryReached !== reached) {
            summaryReached = reached;
            reference.invokeMethodAsync('ActualizarAccesoResumen', reached).catch(() => {});
        }
    };
    let summaryObserver;
    const observeSummary = () => {
        summaryObserver?.disconnect();
        summaryObserver = new IntersectionObserver(updateShortcut, {rootMargin: `0px 0px -${Math.round(window.innerHeight * .45)}px 0px`});
        summaryObserver.observe(summary);
        updateShortcut();
    };
    const viewportChanged = () => requestAnimationFrame(() => { if (!disconnected && element.isConnected) observeSummary(); });
    observeSummary();
    window.addEventListener('resize', viewportChanged);
    const disconnectSummary = () => { disconnected = true; summaryObserver.disconnect(); window.removeEventListener('resize', viewportChanged); };
    const resize = new ResizeObserver(() => {
        updateShortcut();
        const style = getComputedStyle(element), columns = Number(element.dataset.columns);
        const gap = parseFloat(style.getPropertyValue('--seat-gap')) || 4;
        const label = parseFloat(style.getPropertyValue('--seat-row-label')) || 16;
        const padding = parseFloat(style.getPropertyValue('--seat-padding')) || 8;
        const fit = Math.floor((element.clientWidth - label * 2 - padding * 2 - gap * (columns + 1)) / columns);
        reference.invokeMethodAsync('AjustarMapa', Math.max(24, Math.min(44, fit))).then(() => { if (initial) { initial = false; requestAnimationFrame(() => { if (element.isConnected) element.scrollLeft = Math.max(0, (element.scrollWidth - element.clientWidth) / 2); }); } }).catch(() => {});
    });
    const keyboard = event => {
        const seat = event.target.closest('button[data-seat]');
        if (!seat || !['ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Home','End'].includes(event.key)) return;
        const row = Number(seat.dataset.row), col = Number(seat.dataset.col);
        const available = [...element.querySelectorAll('button[data-seat]:not(:disabled)')];
        let target;
        if (event.key === 'Home' || event.key === 'End') {
            const sameRow = available.filter(s => Number(s.dataset.row) === row);
            target = event.key === 'Home' ? sameRow[0] : sameRow.at(-1);
        } else {
            const vertical = event.key === 'ArrowUp' || event.key === 'ArrowDown';
            const direction = event.key === 'ArrowUp' || event.key === 'ArrowLeft' ? -1 : 1;
            target = available.filter(s => vertical ? Number(s.dataset.col) === col && (Number(s.dataset.row) - row) * direction > 0 : Number(s.dataset.row) === row && (Number(s.dataset.col) - col) * direction > 0)
                .sort((a,b) => Math.abs(Number(a.dataset[vertical ? 'row' : 'col']) - (vertical ? row : col)) - Math.abs(Number(b.dataset[vertical ? 'row' : 'col']) - (vertical ? row : col)))[0];
        }
        event.preventDefault();
        if (target) { available.forEach(s => s.tabIndex = s === target ? 0 : -1); target.focus({preventScroll: true}); target.scrollIntoView({block:'nearest', inline:'nearest', behavior:'instant'}); }
    };
    resize.observe(element); element.addEventListener('keydown', keyboard); maps.set(element, {resize,keyboard,disconnectSummary});
}
export function focusSeat(element, id) {
    const seats = [...element.querySelectorAll('button[data-seat]:not(:disabled)')], target = seats.find(s => s.dataset.seat === id);
    if (target) { seats.forEach(s => s.tabIndex = s === target ? 0 : -1); target.focus({preventScroll: true}); }
}
export function disconnectSeatMap(element) { const state = maps.get(element); if (!state) return; state.resize.disconnect(); state.disconnectSummary(); element.removeEventListener('keydown', state.keyboard); maps.delete(element); }

export function centerSeatMap(element) { requestAnimationFrame(() => { if (element.isConnected) element.scrollLeft = Math.max(0, (element.scrollWidth - element.clientWidth) / 2); }); }

export function showSeatSection(element) {
    if (!element?.isConnected) return;
    element.focus({preventScroll: true});
    element.scrollIntoView({block: 'start', behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth'});
}

export async function focusSelectionSummary(element) {
    await new Promise(resolve => requestAnimationFrame(resolve));
    if (!element?.isConnected) return;
    const summary = [...element.closest('.page').querySelectorAll('.seat-summary')].find(item => item.getClientRects().length);
    const target = summary?.querySelector('.seat-clear:not(:disabled)') || summary?.querySelector('h4');
    if (!target) return;
    if (target.tagName === 'H4') target.tabIndex = -1;
    target.focus({preventScroll: true});
}
