
(() => {
    'use strict';
    const input = document.getElementById('cc-site-search');
    if (!input || !input.dataset.apiUrl) return;
    const form = input.closest('form');
    const panel = document.createElement('div');
    panel.id = 'cc-search-results';
    panel.className = 'cc-search-results';
    panel.hidden = true;
    panel.setAttribute('aria-label', 'Gợi ý sản phẩm');
    const status = document.createElement('p');
    status.className = 'cc-search-status';
    status.setAttribute('role', 'status');
    const list = document.createElement('ul');
    const all = document.createElement('a');
    all.className = 'cc-search-all';
    all.textContent = 'Xem tất cả kết quả →';
    panel.append(status, list, all);
    form.append(panel);
    let timer, controller, generation = 0;
    const money = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 });
    function close() {
        clearTimeout(timer);
        controller?.abort();
        generation++;
        panel.hidden = true;
        input.setAttribute('aria-expanded', 'false');
    }
    function open() {
        panel.hidden = false;
        input.setAttribute('aria-expanded', 'true');
    }
    function schedule() {
        close();
        const term = input.value.trim();
        if (term.length < 2 || term.length > 100) return;
        const version = generation;
        timer = setTimeout(async () => {
            controller = new AbortController();
            list.replaceChildren();
            status.textContent = 'Đang tìm sản phẩm…';
            const resultsUrl = new URL(form.action, location.origin);
            resultsUrl.searchParams.set('search', term);
            all.href = resultsUrl.href;
            open();
            try {
                const url = new URL(input.dataset.apiUrl, location.origin);
                url.searchParams.set('search', term);
                const response = await fetch(url, { signal: controller.signal, headers: { Accept: 'application/json' } });
                if (!response.ok) throw new Error('Search unavailable');
                const data = await response.json();
                if (version !== generation) return;
                if (!Array.isArray(data.items)) throw new Error('Invalid response');
                for (const item of data.items) {
                    const li = document.createElement('li');
                    const link = document.createElement('a');
                    link.className = 'cc-search-result';
                    const detail = new URL(input.dataset.detailsUrl, location.origin);
                    detail.searchParams.set('id', String(item.id));
                    link.href = detail.href;
                    const text = document.createElement('span');
                    const title = document.createElement('strong');
                    title.textContent = item.name;
                    const meta = document.createElement('small');
                    meta.textContent = item.brand + ' · ' + (item.inStock ? 'Còn hàng' : 'Tạm hết hàng');
                    text.append(title, meta);
                    const price = document.createElement('span');
                    price.className = 'cc-search-price';
                    price.textContent = money.format(item.price);
                    link.append(text, price);
                    li.append(link);
                    list.append(li);
                }
                status.textContent = data.items.length ? 'Gợi ý phù hợp · tối đa 6 sản phẩm' : 'Chưa có sản phẩm phù hợp. Thử một từ khóa khác.';
            } catch (error) {
                if (error.name === 'AbortError' || version !== generation) return;
                status.textContent = 'Chưa tải được gợi ý. Bạn vẫn có thể nhấn Enter để tìm kiếm.';
            }
        }, 300);
    }
    input.addEventListener('input', event => { if (!event.isComposing) schedule(); });
    input.addEventListener('compositionend', schedule);
    input.addEventListener('focus', schedule);
    form.addEventListener('submit', close);
    form.addEventListener('keydown', event => {
        if (event.key === 'Escape') { close(); input.focus(); close(); }
        if (event.key === 'ArrowDown' && !panel.hidden) {
            const links = [...panel.querySelectorAll('a')];
            const i = links.indexOf(document.activeElement);
            if (i < links.length - 1) { event.preventDefault(); links[i + 1].focus(); }
        }
        if (event.key === 'ArrowUp' && !panel.hidden) {
            const links = [...panel.querySelectorAll('a')];
            const i = links.indexOf(document.activeElement);
            event.preventDefault();
            if (i > 0) links[i - 1].focus(); else input.focus();
        }
    });
    document.addEventListener('pointerdown', event => { if (!form.contains(event.target)) close(); });
    form.addEventListener('focusout', event => { if (!form.contains(event.relatedTarget)) close(); });
})();
