// RaceDay site scripts (no framework needed beyond Bootstrap).
(function () {
    "use strict";

    // ---- Confirmation modal for destructive actions: <form data-confirm="Delete this event?"> ----
    const modalEl = document.getElementById("rdConfirmModal");
    if (modalEl && window.bootstrap) {
        const modal = new bootstrap.Modal(modalEl);
        const body = document.getElementById("rdConfirmBody");
        const ok = document.getElementById("rdConfirmOk");
        let pendingForm = null;
        let pendingSubmitter = null;

        document.addEventListener("submit", function (e) {
            const form = e.target;
            if (!(form instanceof HTMLFormElement) || !form.dataset.confirm || form.dataset.confirmed === "1") return;
            e.preventDefault();
            pendingForm = form;
            pendingSubmitter = e.submitter || null;
            body.textContent = form.dataset.confirm;
            ok.textContent = form.dataset.confirmButton || "Yes, continue";
            ok.className = "btn " + (form.dataset.confirmStyle || "btn-danger");
            modal.show();
        });

        ok.addEventListener("click", function () {
            if (!pendingForm) return;
            pendingForm.dataset.confirmed = "1";
            modal.hide();
            if (pendingForm.requestSubmit) pendingForm.requestSubmit(pendingSubmitter || undefined); else pendingForm.submit();
        });
    }

    // ---- Image preview for file inputs: <input type="file" data-preview="#imgId"> ----
    document.querySelectorAll("input[type=file][data-preview]").forEach(function (input) {
        input.addEventListener("change", function () {
            const target = document.querySelector(input.dataset.preview);
            const file = input.files && input.files[0];
            if (!target || !file) return;
            if (file.size > 5 * 1024 * 1024) {
                alertInline(input, "Images must be 5 MB or smaller.");
                input.value = "";
                return;
            }
            clearInline(input);
            target.src = URL.createObjectURL(file);
            target.classList.remove("d-none");
        });
    });

    // ---- Auto-submit forms when a field changes: <select data-autosubmit> ----
    document.querySelectorAll("[data-autosubmit]").forEach(function (el) {
        el.addEventListener("change", function () { el.form && el.form.submit(); });
    });

    // ---- Category presets: <button data-fill-name="10 km" data-fill-distance="10"> ----
    document.querySelectorAll("[data-fill-name]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            const form = btn.closest("form");
            if (!form) return;
            const name = form.querySelector("[data-field=name]");
            const dist = form.querySelector("[data-field=distance]");
            if (name) name.value = btn.dataset.fillName;
            if (dist && btn.dataset.fillDistance) dist.value = btn.dataset.fillDistance;
            if (name) name.focus();
        });
    });

    // ---- Prevent double submits ----
    document.addEventListener("submit", function (e) {
        const form = e.target;
        if (e.defaultPrevented || !(form instanceof HTMLFormElement)) return;
        if (window.jQuery && jQuery(form).data("validator") && !jQuery(form).valid()) return;
        const btn = e.submitter || form.querySelector("button[type=submit]");
        // Disable after the browser has collected the form data, so the button's own value is still sent.
        if (btn && !btn.dataset.noLock) setTimeout(function () { btn.disabled = true; }, 0);
    });

    function alertInline(input, msg) {
        clearInline(input);
        const div = document.createElement("div");
        div.className = "text-danger small mt-1 rd-inline-error";
        div.textContent = msg;
        input.insertAdjacentElement("afterend", div);
    }
    function clearInline(input) {
        const next = input.parentElement && input.parentElement.querySelector(".rd-inline-error");
        if (next) next.remove();
    }
})();
