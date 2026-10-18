// Application grid.
//
// Each .application-grid (rendered by the ApplicationGrid view component)
// loads its rows from the JSON endpoint in data-url. The server applies the
// role's default filters and echoes the applied state back; that state is
// mirrored into the page URL so refresh and back keep the view.
(() => {
    const badgeClasses = {
        Submitted: 'text-bg-primary',
        Returned: 'text-bg-warning',
        Approved: 'text-bg-success',
        Denied: 'text-bg-danger'
    };

    document.querySelectorAll('.application-grid').forEach(initGrid);

    function initGrid(grid) {
        const dataUrl = grid.dataset.url;
        const showApplicant = grid.dataset.showApplicant === 'true';
        const highlightReturned = grid.dataset.highlightReturned === 'true';

        const form = grid.querySelector('[data-grid-filters]');
        const summary = grid.querySelector('[data-grid-summary]');
        const rows = grid.querySelector('[data-grid-rows]');
        const pager = grid.querySelector('[data-grid-pager]');
        const columnCount = grid.querySelectorAll('thead th').length;

        // Only what the page URL specifies is sent; the server fills the rest.
        let state = Object.fromEntries(new URLSearchParams(window.location.search));

        const load = async () => {
            summary.textContent = 'Loading…';

            let response;

            try {
                response = await fetch(`${dataUrl}?${new URLSearchParams(state)}`, {
                    headers: { Accept: 'application/json' }
                });
            } catch {
                showError();
                return;
            }

            // Signed out: reload so the normal login redirect happens.
            if (response.status === 401) {
                window.location.reload();
                return;
            }

            if (!response.ok) {
                showError();
                return;
            }

            const page = await response.json();

            state = {
                status: page.status,
                sort: page.sort,
                desc: String(page.descending),
                page: String(page.pageNumber)
            };

            if (page.propertyId != null) {
                state.propertyId = String(page.propertyId);
            }

            history.replaceState(null, '', `${window.location.pathname}?${new URLSearchParams(state)}`);
            render(page);
        };

        const render = page => {
            form.elements.status.value = page.status;
            form.elements.propertyId.value = page.propertyId ?? '';

            summary.textContent = `${page.totalCount} application(s)`;

            grid.querySelectorAll('[data-sort]').forEach(button => {
                button.querySelector('[data-sort-indicator]').textContent =
                    button.dataset.sort === page.sort ? (page.descending ? ' ▼' : ' ▲') : '';
            });

            rows.replaceChildren(...(page.items.length > 0
                ? page.items.map(renderRow)
                : [emptyRow()]));

            renderPager(page);
        };

        const renderRow = item => {
            const row = document.createElement('tr');

            row.append(cell(String(item.id)));

            if (showApplicant) {
                row.append(cell(item.applicantName));
            }

            row.append(cell(`${item.propertyName}, unit ${item.unitNumber}`));

            const badge = document.createElement('span');
            const actionNeeded = highlightReturned && item.status === 'Returned';
            badge.className = `badge ${badgeClasses[item.status] ?? 'text-bg-secondary'}`;
            badge.textContent = actionNeeded ? 'Returned – action needed' : item.status;
            row.append(cell(badge));

            row.append(cell(item.submittedAtUtc
                ? new Date(item.submittedAtUtc).toLocaleString(undefined, {
                    dateStyle: 'short',
                    timeStyle: 'short'
                })
                : '—'));

            const link = document.createElement('a');
            link.href = item.detailsUrl;
            link.textContent = 'Open';
            const linkCell = cell(link);
            linkCell.className = 'text-end';
            row.append(linkCell);

            return row;
        };

        const emptyRow = () => {
            const row = document.createElement('tr');
            const message = cell('No applications match these filters.');
            message.colSpan = columnCount;
            row.append(message);
            return row;
        };

        const renderPager = page => {
            if (page.totalPages <= 1) {
                pager.replaceChildren();
                return;
            }

            const list = document.createElement('ul');
            list.className = 'pagination';

            list.append(pageItem('Previous', page.pageNumber - 1, page.pageNumber === 1, false));

            for (let number = 1; number <= page.totalPages; number++) {
                list.append(pageItem(String(number), number, false, number === page.pageNumber));
            }

            list.append(pageItem('Next', page.pageNumber + 1, page.pageNumber === page.totalPages, false));

            pager.replaceChildren(list);
        };

        const pageItem = (label, number, disabled, active) => {
            const item = document.createElement('li');
            item.className = `page-item${disabled ? ' disabled' : ''}${active ? ' active' : ''}`;

            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'page-link';
            button.textContent = label;
            button.disabled = disabled;
            button.addEventListener('click', () => {
                state.page = String(number);
                load();
            });

            item.append(button);
            return item;
        };

        const showError = () => {
            summary.textContent = 'The applications could not be loaded. Refresh the page to try again.';
            rows.replaceChildren();
            pager.replaceChildren();
        };

        // A new column starts ascending; the active column flips direction.
        grid.querySelectorAll('[data-sort]').forEach(button => {
            button.addEventListener('click', () => {
                const field = button.dataset.sort;
                state.desc = String(state.sort === field && state.desc !== 'true');
                state.sort = field;
                state.page = '1';
                load();
            });
        });

        form.addEventListener('submit', event => {
            event.preventDefault();

            state.status = form.elements.status.value;
            state.page = '1';

            if (form.elements.propertyId.value) {
                state.propertyId = form.elements.propertyId.value;
            } else {
                delete state.propertyId;
            }

            load();
        });

        load();
    }

    function cell(content) {
        const td = document.createElement('td');
        td.append(content);
        return td;
    }
})();
