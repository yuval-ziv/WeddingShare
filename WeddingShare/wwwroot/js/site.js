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

function setCookie(cname, cvalue, hours) {
    const d = new Date();
    d.setTime(d.getTime() + (hours * 60 * 60 * 1000));
    document.cookie = `${cname}=${cvalue};expires=${d.toUTCString()};path=/`;
}

function getCookie(cname) {
    let ca = document.cookie.split(';');
    let name = `${cname}=`;

    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];

        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }

        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }

    return "";
}

function displayMessage(title, message, errors) {
    hideLoader();

    $('#alert-message-modal .modal-title').text(title);
    $('#alert-message-modal .modal-message').html(message);

    $('#alert-message-modal .modal-error').hide();
    if (errors && errors.length > 0) {
        var errorMessage = `<b>${localization.translate('Errors')}:</b>`;
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

function displayIdentityCheck() {
    let identity = getCookie('ViewerIdentity');
    if (identity !== undefined && identity.length > 0) {
        return;
    }

    displayPopup({
        Title: localization.translate('Identity_Check'),
        Fields: [{
            Id: 'identity-name',
            Name: localization.translate('Identity_Check_Name'),
            Value: '',
            Hint: localization.translate('Identity_Check_Hint'),
            Placeholder: localization.translate('Identity_Check_Placeholder')
        }],
        Buttons: [{
            Text: localization.translate('Identity_Check_Tell_Us'),
            Class: 'btn-success',
            Callback: function () {
                let name = $('#popup-modal-field-identity-name').val().trim();
                if (name !== undefined && name.length > 0) {
                    setCookie('ViewerIdentity', name, 24);
                    window.location.reload();
                }
            }
        }, {
            Text: localization.translate('Identity_Check_Stay_Anonymous'),
            Callback: function () {
                setCookie('ViewerIdentity', 'Anonymous', 1);
            }
        }]
    });
}

(function () {
    document.addEventListener('DOMContentLoaded', function () {
        $(document).off('click', '.change-theme').on('click', '.change-theme', function (e) {
            var currentTheme = getCookie('Theme');
            if (currentTheme === 'dark') {
                setCookie('Theme', 'default', 24);
            } else {
                setCookie('Theme', 'dark', 24);
            }

            window.location.reload();
        });

        $(document).off('click', '.change-identity').on('click', '.change-identity', function (e) {
            preventDefaults(e);
            displayPopup({
                Title: localization.translate('Identity_Check_Change_Identity'),
                Fields: [{
                    Id: 'identity-name',
                    Name: localization.translate('Identity_Check_Name'),
                    Value: getCookie('ViewerIdentity'),
                    Hint: localization.translate('Identity_Check_Hint'),
                    Placeholder: localization.translate('Identity_Check_Placeholder')
                }],
                Buttons: [{
                    Text: localization.translate('Identity_Check_Change'),
                    Class: 'btn-success',
                    Callback: function () {
                        let name = $('#popup-modal-field-identity-name').val().trim();
                        if (name !== undefined && name.length > 0) {
                            setCookie('ViewerIdentity', name, 24);
                        }
                    }
                }, {
                    Text: localization.translate('Cancel')
                }]
            });
        });

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
                displayMessage(localization.translate('Gallery'), localization.translate('Gallery_Invalid_Name'));
            }
        });

        $(document).off('submit', '#frmAdminLogin').on('submit', '#frmAdminLogin', function (e) {
            preventDefaults(e);

            var username = $('#frmAdminLogin input.input-username').val();
            if (username === undefined || username.length === 0) {
                displayMessage(localization.translate('Login'), localization.translate('Login_Invalid_Username'));
                return;
            }

            var password = $('#frmAdminLogin input.input-password').val();
            if (password === undefined || password.length === 0) {
                displayMessage(localization.translate('Login'), localization.translate('Login_Invalid_Password'));
                return;
            }

            displayLoader(localization.translate('Loading'));

            $.ajax({
                url: '/Admin/Login',
                method: 'POST',
                data: { Username: username, Password: password }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true) {
                        window.location = `/Admin`;
                    } else if (data.message) {
                        displayMessage(localization.translate('Login'), localization.translate('Login_Failed'), [data.message]);
                    } else {
                        displayMessage(localization.translate('Login'), localization.translate('Login_Invalid_Details'));
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(localization.translate('Login'), localization.translate('Login_Failed'), [error]);
                });
        });

        $(document).off('click', 'button.btnDismissPopup').on('click', 'button.btnDismissPopup', function (e) {
            preventDefaults(e);
            hidePopup($(this).closest('.modal').attr('id'));
        });

    });
})();