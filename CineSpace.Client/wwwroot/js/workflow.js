// Feedback follows real API work; seat and cart interactions stay unobstructed.
window.cineWork = (() => {
    let clicked, serial = 0, active = 0, revision = 0;
    const requests = new Map(), buttons = new Map();
    document.addEventListener('click', e => { clicked = e.target.closest('button'); }, true);
    document.addEventListener('submit', e => { clicked = e.submitter || e.target.querySelector('button[type="submit"]'); }, true);
    const eligible = b => b?.isConnected && !b.closest('[data-booking-step], .order-quantity, .order-cart-items, .confectionery-edge-cart, .confectionery-page') && !b.classList.contains('menu-toggle-btn');
    return {
        begin() {
            const id = ++serial, button = eligible(clicked) ? clicked : null;
            if (button) {
                if (!buttons.has(button)) buttons.set(button, { count:0, disabled:button.disabled });
                buttons.get(button).count++;
                button.disabled = true; button.classList.add('is-requesting'); button.setAttribute('aria-busy','true');
            }
            requests.set(id,button); active++; revision++; document.documentElement.classList.add('has-api-request'); return id;
        },
        end(id) {
            if (!requests.has(id)) return;
            const b = requests.get(id); requests.delete(id);
            if (b && buttons.has(b)) buttons.get(b).count--;
            if (--active === 0) {
                const completed = revision;
                requestAnimationFrame(() => requestAnimationFrame(() => {
                    if (active || revision !== completed) return;
                    for (const [button,state] of buttons) {
                        if (!state.disabled) button.disabled = false;
                        button.classList.remove('is-requesting'); button.removeAttribute('aria-busy');
                    }
                    buttons.clear(); clicked = null; document.documentElement.classList.remove('has-api-request');
                }));
            }
        },
        paint: () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))
    };
})();
