// Modal forms.
//
// An element with data-modal-url opens the shared #app-modal and loads the
// partial view returned by that URL. Forms inside the modal post via fetch:
//   - 204 No Content: success. Close the modal and reload the element named
//     by the trigger's data-refresh-target from its data-refresh-url, or
//     the whole page when the trigger names no target.
//   - HTML: the server re-rendered the form (e.g. validation errors), so it
//     replaces the modal content in place.
(() => {
    const modalElement = document.getElementById('app-modal');

    if (!modalElement) {
        return;
    }

    const modal = new bootstrap.Modal(modalElement);
    const content = modalElement.querySelector('.modal-content');
    let refreshTarget = null;

    const errorHtml =
        '<div class="modal-body"><p class="text-danger mb-0">' +
        'Something went wrong. Refresh the page and try again.</p></div>';

    const showContent = html => {
        content.innerHTML = html;

        const form = content.querySelector('form');

        if (form && window.jQuery?.validator?.unobtrusive) {
            jQuery.validator.unobtrusive.parse(form);
        }
    };

    // Without a refresh target the change affects the whole page
    // (e.g. a status change), so reload it.
    const refresh = async () => {
        if (!refreshTarget) {
            window.location.reload();
            return;
        }

        const response = await fetch(refreshTarget.dataset.refreshUrl);

        if (response.ok) {
            refreshTarget.innerHTML = await response.text();
        } else {
            window.location.reload();
        }
    };

    document.addEventListener('click', async event => {
        const trigger = event.target.closest('[data-modal-url]');

        if (!trigger) {
            return;
        }

        event.preventDefault();

        refreshTarget = trigger.dataset.refreshTarget
            ? document.querySelector(trigger.dataset.refreshTarget)
            : null;

        const response = await fetch(trigger.dataset.modalUrl);

        showContent(response.ok ? await response.text() : errorHtml);
        modal.show();
    });

    content.addEventListener('submit', async event => {
        const form = event.target;

        event.preventDefault();

        if (window.jQuery?.validator && !jQuery(form).valid()) {
            return;
        }

        const response = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form)
        });

        if (response.status === 204) {
            modal.hide();
            await refresh();
            return;
        }

        showContent(response.ok ? await response.text() : errorHtml);
    });
})();
