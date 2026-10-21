export function connect(root, reference) {
    const trigger = () => root.querySelector('.date-entry button[aria-expanded="true"]');
    const close = () => reference.invokeMethodAsync('Cerrar').catch(() => {});
    const outside = event => {
        const panel = root.querySelector('.date-calendar');
        if (panel && !panel.contains(event.target) && !(root.contains(event.target) && event.target.closest('.date-entry button'))) close();
    };
    const keydown = event => {
        if (!root.querySelector('.date-calendar')) return;
        if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); trigger()?.focus(); close(); return; }
        const day = event.target.closest('.range-calendar button');
        if (!day) return;
        const days = [...root.querySelectorAll('.range-calendar button')];
        const index = days.indexOf(day);
        let next = index;
        if (event.key === 'ArrowRight') next++;
        else if (event.key === 'ArrowLeft') next--;
        else if (event.key === 'ArrowDown') next += 7;
        else if (event.key === 'ArrowUp') next -= 7;
        else if (event.key === 'Home') next = days.indexOf(day.closest('tr').querySelector('button'));
        else if (event.key === 'End') next = days.indexOf([...day.closest('tr').querySelectorAll('button')].at(-1));
        else return;
        event.preventDefault();
        if (next >= 0 && next < days.length && !days[next].disabled) days[next].focus();
    };
    document.addEventListener('pointerdown', outside);
    document.addEventListener('focusin', outside);
    root.addEventListener('keydown', keydown);
    return {
        focusTrigger() { trigger()?.focus({preventScroll: true}); },
        focusCalendar() {
            const panel = root.querySelector('.date-calendar');
            const day = panel?.querySelector('[aria-pressed="true"]:not(:disabled)') || panel?.querySelector('.range-calendar button:not(:disabled)');
            panel?.scrollIntoView({block: 'nearest', behavior: 'instant'});
            day?.focus({preventScroll: true});
            if (day && panel.scrollHeight > panel.clientHeight) panel.scrollTop += day.getBoundingClientRect().top - panel.getBoundingClientRect().top - panel.clientHeight / 2;
        },
        dispose() { document.removeEventListener('pointerdown', outside); document.removeEventListener('focusin', outside); root.removeEventListener('keydown', keydown); }
    };
}
