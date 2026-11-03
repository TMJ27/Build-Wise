'use strict';
(() => {
    const menu = document.querySelector('#workspace-navigation');
    const toggle = document.querySelector('[data-workspace-menu]');
    document.querySelector('.skip-link')?.addEventListener('click', event => {
        event.preventDefault();
        document.querySelector('#main-content')?.focus();
    });
    function closeNavigation() {
        menu?.classList.remove('open');
        document.body.classList.remove('workspace-navigation-open');
        toggle?.setAttribute('aria-expanded', 'false');
        toggle?.setAttribute('aria-label', 'Open workspace navigation');
    }
    toggle?.addEventListener('click', () => {
        if (!menu) return;
        const open = menu.classList.toggle('open');
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'Close workspace navigation' : 'Open workspace navigation');
        document.body.classList.toggle('workspace-navigation-open', open);
        if (open) menu.querySelector('a')?.focus();
    });
    document.addEventListener('click', event => {
        if (event.target.closest('[data-action="notifications"]')) {
            const content = document.querySelector('#modal-content');
            const template = document.querySelector('#dashboard-notifications');
            if (content) {
                if (template) content.replaceChildren(template.content.cloneNode(true));
                else {
                    const title = document.createElement('h2');
                    title.textContent = 'Notifications';
                    const message = document.createElement('p');
                    message.textContent = 'You have no new notifications.';
                    const close = document.createElement('button');
                    close.className = 'btn'; close.type = 'button';
                    close.dataset.workspaceDialogClose = ''; close.textContent = 'Close';
                    content.replaceChildren(title, message, close);
                }
                document.querySelector('#modal')?.showModal();
            }
        }
        if (event.target.closest('[data-workspace-dialog-close]')) document.querySelector('#modal')?.close();
        if (menu && !menu.contains(event.target) && !toggle?.contains(event.target)) closeNavigation();
        if (event.target.closest('#workspace-navigation a')) closeNavigation();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && menu?.classList.contains('open')) {
            closeNavigation(); toggle?.focus();
        }
    });
    window.matchMedia('(min-width: 901px)').addEventListener('change', closeNavigation);
})();
