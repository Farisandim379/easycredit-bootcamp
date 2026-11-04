// EasyCredit Frontend Core Utilities (EC)
window.EC = {
    // Ambil anti-forgery token dari halaman
    token: function () {
        return $('input[name="__RequestVerificationToken"]').first().val() || '';
    },

    // Modal konfirmasi (Promise-based)
    confirm: function (options) {
        options = options || {};
        var title = options.title || 'Konfirmasi';
        var message = options.message || 'Apakah Anda yakin ingin melanjutkan?';
        var confirmText = options.confirmText || 'Ya, lanjutkan';
        var danger = options.danger !== false;

        return new Promise(function (resolve) {
            var modalEl = document.getElementById('confirmModal');
            if (!modalEl) {
                // Fallback jika modal HTML belum ada di DOM
                var ok = window.confirm(message);
                resolve(ok);
                return;
            }

            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            $('#confirmTitle').text(title);
            $('#confirmMessage').text(message);

            var $okBtn = $('#confirmOk');
            $okBtn.text(confirmText);
            if (danger) {
                $okBtn.removeClass('btn-primary').addClass('btn-danger');
            } else {
                $okBtn.removeClass('btn-danger').addClass('btn-primary');
            }

            // Bersihkan event handler lama sebelum pasang baru
            $okBtn.off('click').one('click', function () {
                modal.hide();
                resolve(true);
            });

            $(modalEl).off('hidden.bs.modal').one('hidden.bs.modal', function () {
                resolve(false);
            });

            modal.show();
        });
    },

    // Toast notification
    toast: function (type, message, title) {
        var $stack = $('#toastStack');
        if (!$stack.length) {
            $stack = $('<div id="toastStack" class="toast-stack" aria-live="polite"></div>').appendTo('body');
        }

        var isSuccess = type === 'success';
        var toastClass = isSuccess ? 'is-success' : 'is-error';
        var titleText = title || (isSuccess ? 'Berhasil' : 'Gagal');

        var $toast = $(
            '<div class="app-toast ' + toastClass + '">' +
            '  <span class="toast-dot"></span>' +
            '  <div class="toast-body">' +
            '    <strong>' + $('<div>').text(titleText).html() + '</strong>' +
            '    <span>' + $('<div>').text(message || '').html() + '</span>' +
            '  </div>' +
            '</div>'
        );

        $stack.append($toast);

        setTimeout(function () {
            $toast.addClass('is-leaving');
            setTimeout(function () {
                $toast.remove();
            }, 200);
        }, 3500);
    }
};
