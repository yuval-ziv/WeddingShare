const preventDefaults = event => {
    event.preventDefault();
    event.stopPropagation();
};

function displayLoader(message) {
    $('body').loading({
        theme: 'dark',
        message,
        stoppable: false,
        start: true
    });
}

function hideLoader() {
    $('body').loading('stop');
}

function uuidv4() {
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
        (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
    );
}

function displayMessage(title, message, errors) {
    hideLoader();

    $('#alert-message-modal .modal-title').text(title);
    $('#alert-message-modal .modal-message').html(message);

    $('#alert-message-modal .modal-error').hide();
    if (errors && errors.length > 0) {
        var errorMessage = `<b>Errors:</b>`;
        errorMessage += `<ul>`;
        errors.forEach((error) => {
            errorMessage += `<li>${error}</li>`;
        });
        errorMessage += `</ul>`;
        $('#alert-message-modal .modal-error').html(errorMessage);
        $('#alert-message-modal .modal-error').show();
    } else {
        $('#alert-message-modal .modal-error').html('');
    }

    $('#alert-message-modal').modal('show');
}

lightbox.option({
    'disableScrolling': true
});

(function () {
    document.addEventListener('DOMContentLoaded', function () {

        $(document).off('click', '.btn-reload').on('click', '.btn-reload', function () {
            window.location.reload();
        });

        $(document).off('click', '#btnGenerateGalleryName').on('click', '#btnGenerateGalleryName', function (e) {
            preventDefaults(e);
            $('input#gallery-id').val(uuidv4());
        });

        $(document).off('submit', '#frmSelectGallery').on('submit', '#frmSelectGallery', function (e) {
            preventDefaults(e);

            var galleryId = $('input#gallery-id').val();
            var secretKey = $('input#gallery-key').val();
            if (galleryId && galleryId.length > 0) {
                var url = `/Gallery?id=${galleryId}`;
                if (secretKey && secretKey.length > 0) {
                    url = `${url}&key=${secretKey}`;
                }

                window.location = url;
            } else {
                displayMessage(`Gallery`, `Please select a valid gallery name`);
            }
        });

        $(document).off('submit', '#frmAdminLogin').on('submit', '#frmAdminLogin', function (e) {
            preventDefaults(e);

            var password = $('input#admin-password').val();
            if (password === undefined || password.length === 0) {
                displayMessage(`Login`, `Please enter a valid password`);
                return;
            }

            displayLoader('Loading...');

            $.ajax({
                url: '/Admin/Login',
                method: 'POST',
                data: { Password: password }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true) {
                        window.location = `/Admin`;
                    } else if (data.message) {
                        displayMessage(`Login`, `Login failed`, [data.message]);
                    } else {
                        displayMessage(`Login`, `Invalid username or password specified`);
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(`Login`, `Login failed`, [error]);
                });
        });

        $(document).off('click', 'button.btnDismissPopup').on('click', 'button.btnDismissPopup', function (e) {
            preventDefaults(e);
            hidePopup($(this).closest('.modal').attr('id'));
        });

    });
})();