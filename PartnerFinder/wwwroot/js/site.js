// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Show a full-screen "processing" overlay when a long-running form is submitted
// (AI research takes ~30-60s and would otherwise leave the page looking frozen).
// Any <form data-longtask="message"> triggers it.
(function () {
    function showOverlay(message) {
        var overlay = document.getElementById('pf-overlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'pf-overlay';
            overlay.innerHTML =
                '<div class="pf-box">' +
                '<div class="spinner-border text-primary" style="width:3.5rem;height:3.5rem;" role="status">' +
                '<span class="visually-hidden">Working…</span></div>' +
                '<h4 class="mt-4 mb-2" id="pf-overlay-msg"></h4>' +
                '<p class="text-muted mb-0">Please keep this tab open — the page will refresh automatically when it is done.</p>' +
                '<div class="progress mt-4" style="height:8px;">' +
                '<div class="progress-bar progress-bar-striped progress-bar-animated" style="width:100%"></div>' +
                '</div></div>';
            document.body.appendChild(overlay);
        }
        document.getElementById('pf-overlay-msg').textContent = message || 'Working…';
        overlay.classList.add('show');
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (form && form.hasAttribute && form.hasAttribute('data-longtask')) {
            showOverlay(form.getAttribute('data-longtask'));
        }
    });
})();
