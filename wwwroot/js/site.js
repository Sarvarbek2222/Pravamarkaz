(function () {
    'use strict';

    // ---- Mavzu (yorug' / qorong'i) ----
    var root = document.documentElement;
    var themeBtn = document.getElementById('themeToggle');
    function syncThemeIcon() {
        if (!themeBtn) return;
        var dark = root.getAttribute('data-bs-theme') === 'dark';
        themeBtn.innerHTML = dark ? '<i class="bi bi-sun"></i>' : '<i class="bi bi-moon-stars"></i>';
    }
    if (themeBtn) {
        syncThemeIcon();
        themeBtn.addEventListener('click', function () {
            var next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            root.setAttribute('data-bs-theme', next);
            try { localStorage.setItem('pm-theme', next); } catch (e) { }
            syncThemeIcon();
            document.dispatchEvent(new CustomEvent('pm:theme', { detail: next }));
        });
    }

    // ---- Mobil yon menyu ----
    var sidebar = document.getElementById('sidebar');
    var backdrop = document.getElementById('sidebarBackdrop');
    var toggle = document.getElementById('sidebarToggle');
    function closeSidebar() { sidebar && sidebar.classList.remove('open'); backdrop && backdrop.classList.remove('show'); }
    if (toggle) toggle.addEventListener('click', function () { sidebar.classList.add('open'); backdrop.classList.add('show'); });
    if (backdrop) backdrop.addEventListener('click', closeSidebar);

    // ---- Toast xabarlar ----
    document.querySelectorAll('.pm-toast').forEach(function (t) {
        var close = function () { t.style.transition = 'opacity .2s'; t.style.opacity = 0; setTimeout(function () { t.remove(); }, 200); };
        var btn = t.querySelector('[data-dismiss-toast]');
        if (btn) btn.addEventListener('click', close);
        var ms = parseInt(t.getAttribute('data-autohide'), 10);
        if (ms) setTimeout(close, ms);
    });

    // ---- Jadval qatorini bosganda ochish ----
    document.addEventListener('click', function (e) {
        var row = e.target.closest('tr[data-href]');
        if (!row || e.target.closest('a, button, input, select, form, label')) return;
        if (e.ctrlKey || e.metaKey) window.open(row.dataset.href, '_blank');
        else window.location = row.dataset.href;
    });

    // ---- Tasdiqlash oynasi (data-confirm bo'lgan formalar) ----
    var modalEl = document.getElementById('confirmModal');
    var pendingForm = null;
    document.addEventListener('submit', function (e) {
        var form = e.target;
        var msg = form.getAttribute('data-confirm');
        if (!msg || form.dataset.confirmed === '1') return;
        e.preventDefault();
        if (!modalEl || !window.bootstrap) { if (confirm(msg)) { form.dataset.confirmed = '1'; form.submit(); } return; }
        pendingForm = form;
        document.getElementById('confirmText').textContent = msg;
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    });
    var okBtn = document.getElementById('confirmOk');
    if (okBtn) okBtn.addEventListener('click', function () {
        if (!pendingForm) return;
        pendingForm.dataset.confirmed = '1';
        bootstrap.Modal.getInstance(modalEl).hide();
        pendingForm.submit();
    });

    // ---- Summa maydonlari: "1 500 000" ko'rinishida yozish ----
    function groupDigits(v) {
        var digits = String(v).replace(/[^\d]/g, '');
        if (!digits) return '';
        return digits.replace(/^0+(?=\d)/, '').replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    }
    window.pmGroupDigits = groupDigits;
    document.querySelectorAll('input[data-money]').forEach(function (input) {
        input.setAttribute('inputmode', 'numeric');
        input.setAttribute('autocomplete', 'off');
        input.value = groupDigits(input.value);
        input.addEventListener('input', function () {
            var fromEnd = input.value.length - input.selectionStart;
            input.value = groupDigits(input.value);
            var pos = Math.max(0, input.value.length - fromEnd);
            input.setSelectionRange(pos, pos);
        });
    });
    // To'liq summani bir bosishda qo'yish
    document.querySelectorAll('[data-fill-amount]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var target = document.querySelector(btn.getAttribute('data-target'));
            if (target) { target.value = groupDigits(btn.getAttribute('data-fill-amount')); target.focus(); }
        });
    });

    // ---- Rasm tanlash va oldindan ko'rish ----
    document.querySelectorAll('[data-photo-drop]').forEach(function (box) {
        var input = box.querySelector('input[type=file]');
        var img = box.querySelector('img');
        var ph = box.querySelector('.ph');
        function show(file) {
            if (!file || !file.type.startsWith('image/')) return;
            img.src = URL.createObjectURL(file);
            img.classList.remove('d-none');
            if (ph) ph.classList.add('d-none');
            var rm = document.getElementById('removePhoto');
            if (rm) rm.checked = false;
        }
        box.addEventListener('click', function (e) { if (e.target !== input) input.click(); });
        input.addEventListener('change', function () { show(input.files[0]); });
        ['dragenter', 'dragover'].forEach(function (ev) { box.addEventListener(ev, function (e) { e.preventDefault(); box.classList.add('drag'); }); });
        ['dragleave', 'drop'].forEach(function (ev) { box.addEventListener(ev, function (e) { e.preventDefault(); box.classList.remove('drag'); }); });
        box.addEventListener('drop', function (e) {
            if (e.dataTransfer.files.length) { input.files = e.dataTransfer.files; show(input.files[0]); }
        });
    });

    // ---- Avtomatik yuboriladigan filtrlar ----
    document.querySelectorAll('[data-autosubmit]').forEach(function (el) {
        el.addEventListener('change', function () { el.form.submit(); });
    });

    // ---- Tooltiplar ----
    if (window.bootstrap) document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) { new bootstrap.Tooltip(el); });
})();
