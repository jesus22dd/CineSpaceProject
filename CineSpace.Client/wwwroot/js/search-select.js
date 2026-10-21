export function connect(root, reference) {
    const trigger = () => root.querySelector('[role="combobox"]');
    const close = () => { if (trigger()?.getAttribute('aria-expanded') === 'true') reference.invokeMethodAsync('Cerrar').catch(() => {}); };
    const outside = event => { if (!root.contains(event.target)) close(); };
    const keydown = event => {
        const options = [...root.querySelectorAll('[role="option"]')];
        const index = options.indexOf(event.target);
        if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(); trigger()?.focus(); }
        else if (event.key === 'Enter' && event.target.matches('input')) event.preventDefault();
        else if (['ArrowDown', 'ArrowUp'].includes(event.key) && options.length) { event.preventDefault(); const next = index < 0 ? (event.key === 'ArrowDown' ? 0 : options.length - 1) : (index + (event.key === 'ArrowDown' ? 1 : -1) + options.length) % options.length; options[next].focus(); }
        else if (index >= 0 && ['Home', 'End'].includes(event.key)) { event.preventDefault(); options[event.key === 'Home' ? 0 : options.length - 1].focus(); }
    };
    root.addEventListener('keydown', keydown);
    document.addEventListener('pointerdown', outside);
    document.addEventListener('focusin', outside);
    return { focusSearch() { root.querySelector('input')?.focus(); }, focusTrigger() { trigger()?.focus(); }, dispose() { root.removeEventListener('keydown', keydown); document.removeEventListener('pointerdown', outside); document.removeEventListener('focusin', outside); } };
}
