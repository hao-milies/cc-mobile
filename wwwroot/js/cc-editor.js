
(() => {
    'use strict';
    const source = document.getElementById('Description');
    const host = document.getElementById('cc-description-enhanced');
    if (!source || !host) return;
    const prefix = 'CCMOBILE-DELTA-V1:';
    const error = document.getElementById('cc-description-error');
    const count = document.getElementById('cc-description-count');
    function fail(message) { error.textContent = message; error.hidden = false; }
    if (!window.Quill) {
        if (source.value.startsWith(prefix)) {
            source.readOnly = true;
            fail('Không tải được trình soạn thảo. Vui lòng tải lại trang trước khi sửa mô tả đã định dạng.');
        } else fail('Không tải được trình soạn thảo. Bạn vẫn có thể nhập mô tả văn bản thường.');
        return;
    }
    const editor = new Quill('#cc-description-editor', {
        theme: 'snow',
        placeholder: 'Giới thiệu tính năng, thiết kế và những điểm cần biết về sản phẩm…',
        formats: ['bold', 'italic', 'header', 'list', 'link'],
        modules: { toolbar: { container: '#cc-description-toolbar',
            handlers: { link(value) {
                if (!value) { this.quill.format('link', false); return; }
                const range = this.quill.getSelection();
                if (!range || range.length === 0) {
                    fail('Hãy chọn đoạn chữ cần gắn liên kết.'); return;
                }
                const input = window.prompt('Nhập liên kết bắt đầu bằng https:// hoặc http://');
                if (input === null) return;
                try {
                    const url = new URL(input.trim());
                    if (!['https:', 'http:'].includes(url.protocol)) throw new Error();
                    this.quill.format('link', url.href);
                    error.hidden = true;
                } catch { fail('Liên kết phải bắt đầu bằng https:// hoặc http://'); }
            } }
        } }
    });
    const editable = host.querySelector('.ql-editor');
    editable.id = 'cc-description-input';
    editable.setAttribute('role', 'textbox');
    editable.setAttribute('aria-multiline', 'true');
    editable.setAttribute('aria-labelledby', 'cc-description-label');
    editable.setAttribute('aria-describedby', 'cc-description-help cc-description-error');
    try {
        if (source.value.startsWith(prefix)) {
            const data = JSON.parse(source.value.slice(prefix.length));
            if (!Array.isArray(data.ops)) throw new Error();
            const ops = data.ops.map(op => {
                if (typeof op.insert !== 'string') throw new Error();
                const a = op.attributes || {}, safe = {};
                if (a.bold === true) safe.bold = true;
                if (a.italic === true) safe.italic = true;
                if ([2, 3].includes(a.header)) safe.header = a.header;
                if (['ordered', 'bullet'].includes(a.list)) safe.list = a.list;
                if (typeof a.link === 'string') {
                    try { const u = new URL(a.link); if (['http:', 'https:'].includes(u.protocol)) safe.link = u.href; } catch {}
                }
                return { insert: op.insert, attributes: safe };
            });
            editor.setContents({ ops });
        } else editor.setText(source.value);
    } catch {
        fail('Không đọc được mô tả đã định dạng. Nội dung gốc được giữ lại; vui lòng kiểm tra trước khi sửa.');
        source.readOnly = true;
        return;
    }
    host.hidden = false;
    source.hidden = true;
    document.getElementById('cc-description-label').htmlFor = editable.id;
    editor.history.clear();
    function sync() {
        const length = editor.getLength();
        count.textContent = Math.max(0, length - 1).toLocaleString('vi-VN') + ' / 20.000 ký tự';
        const value = editor.getText().trim() ? prefix + JSON.stringify(editor.getContents()) : '';
        source.value = value;
        // The server validates text length including Quill's final newline.
        const valid = length <= 20000 && value.length <= 200000 && editor.getContents().ops.length <= 4000;
        if (!valid) fail('Mô tả quá dài hoặc định dạng quá phức tạp. Vui lòng rút gọn nội dung.');
        else error.hidden = true;
        return valid;
    }
    editor.on('text-change', sync);
    source.form.addEventListener('submit', event => {
        if (!sync()) { event.preventDefault(); editor.focus(); }
    });
    count.textContent = Math.max(0, editor.getLength() - 1).toLocaleString('vi-VN') + ' / 20.000 ký tự';
})();
