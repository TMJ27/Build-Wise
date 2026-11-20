'use strict';

(() => {
    const panel = document.querySelector('[data-dashboard-table]');
    if (!panel) return;

    const region = panel.querySelector('#dashboard-records-region');
    const viewAll = panel.querySelector('[data-records-view-all]');
    const tabs = panel.querySelectorAll('[data-records-tab]');
    const error = panel.querySelector('[data-records-error]');
    let pendingRequest;

    async function loadRecords(link) {
        const tab = link.dataset.recordsTab;
        pendingRequest?.abort();
        const request = new AbortController();
        pendingRequest = request;

        const url = new URL(link.href, location.href);
        for (const name of [...url.searchParams.keys()]) {
            if (name.toLowerCase() === 'tab' || name.toLowerCase() === 'handler') {
                url.searchParams.delete(name);
            }
        }
        url.searchParams.set('Tab', tab);
        url.searchParams.set('handler', 'Records');
        region.setAttribute('aria-busy', 'true');
        error.hidden = true;

        try {
            const response = await fetch(url, {
                signal: request.signal,
                headers: { Accept: 'text/html' }
            });

            if (response.redirected) {
                location.assign(response.url);
                return;
            }
            if (!response.ok || response.headers.get('X-Dashboard-Records') !== 'true') {
                throw new Error('Unable to load dashboard records.');
            }

            const html = await response.text();
            if (request.signal.aborted) return;
            region.innerHTML = html;
            const selectedTab = document.querySelector('[data-dashboard-range-form] input[name="Tab"]');
            if (selectedTab) selectedTab.value = tab;
            viewAll.href = region.querySelector('[data-view-all-url]').dataset.viewAllUrl;
            viewAll.setAttribute('aria-label', `View all ${tab}`);

            url.searchParams.delete('handler');
            history.replaceState(history.state, '', url);
            for (const link of tabs) {
                const active = link.dataset.recordsTab === tab;
                link.classList.toggle('active', active);
                if (active) link.setAttribute('aria-current', 'page');
                else link.removeAttribute('aria-current');

                const target = new URL(url);
                target.searchParams.set('Tab', link.dataset.recordsTab);
                link.href = target.href;
            }
        } catch (failure) {
            if (failure.name !== 'AbortError') {
                error.textContent = 'Could not load the records. Please try again.';
                error.hidden = false;
            }
        } finally {
            if (pendingRequest === request) {
                region.setAttribute('aria-busy', 'false');
            }
        }
    }

    for (const link of tabs) {
        link.addEventListener('click', event => {
            if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            event.preventDefault();
            loadRecords(link);
        });
    }

})();
