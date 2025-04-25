function reviewPhoto(element, action) {
    var id = element.parent('.btn-group').data('id');
    if (!id) {
        displayMessage(localization.translate('Review'), localization.translate('Review_Id_Missing'));
        return;
    }

    displayLoader(localization.translate('Loading'));

    $.ajax({
        url: '/Admin/ReviewPhoto',
        method: 'POST',
        data: { id, action }
    })
        .done(data => {
            hideLoader();

            if (data.success === true) {
                element.closest('.pending-approval').remove();
                updateGalleryList();
                if ($('.pending-approval').length == 0) {
                    updatePendingReviews();
                }
            } else if (data.message) {
                displayMessage(localization.translate('Review'), localization.translate('Review_Failed'), [data.message]);
            }
        })
        .fail((xhr, error) => {
            hideLoader();
            displayMessage(localization.translate('Review'), localization.translate('Review_Failed'), [error]);
        });
}

function updateUsersList() {
    $.ajax({
        type: 'GET',
        url: `/Admin/UsersList`,
        success: function (data) {
            $('#users-list').html(data);
        }
    });
}

function updateGalleryList() {
    $.ajax({
        type: 'GET',
        url: `/Admin/GalleriesList`,
        success: function (data) {
            $('#galleries-list').html(data);
        }
    });
}

function updatePendingReviews() {
    $.ajax({
        type: 'GET',
        url: `/Admin/PendingReviews`,
        success: function (data) {
            $('#pending-reviews').html(data);
        }
    });
}

function updatePage() {
    updateUsersList();
    updateGalleryList();
    updatePendingReviews();
}

function initPasswordValidation() {
    if ($('.password-validator').length > 0) {
        $('.password-validator').each(function () {
            let validator = $(this);
            let input = $(validator.data('input'));
            if (input !== undefined && input.length > 0) {
                let confirmField = input.parent().parent().parent().find('input.confirm-password');
                if (confirmField !== undefined && confirmField.length === 1) {
                    validator.find('.lbl-confirm').removeClass('visually-hidden');
                    confirmField.off('keyup').on('keyup', function () {
                        var value = $(input).val();
                        setPasswordValidationField(validator.find('.lbl-confirm'), confirmField.val() === value && value.length);
                        setPasswordValidationField(validator, validator.find('li[class^=\'lbl-\']:not([class*=\'hidden\'])').length === 0);
                    });
                }

                $(input).off('keyup').on('keyup', function () {
                    var value = $(this).val();
                    setPasswordValidationField(validator.find('.lbl-lower') , /[a-z]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-upper') , /[A-Z]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-number'), /[0-9]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-special'), /[^A-Za-z0-9]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-length'), value.length >= 8);

                    if (confirmField !== undefined && confirmField.length === 1) {
                        setPasswordValidationField(validator.find('.lbl-confirm'), confirmField.val() === value && value.length);
                    }

                    setPasswordValidationField(validator, validator.find('li[class^=\'lbl-\']:not([class*=\'hidden\'])').length === 0);
                })
            }
        });
    }
}

function setPasswordValidationField(field, valid) {
    if (valid) {
        field.addClass('visually-hidden');
    } else {
        field.removeClass('visually-hidden');
    }
}

function selectActiveTab(tab) {
    tab = tab.replace('#', '');

    if (tab === undefined || tab === null || tab.length === 0) {
        tab = $('a.pnl-selector')[0].attributes['data-tab'].value;
    }

    $('a.pnl-selector').removeClass('active');
    $(`a.pnl-selector[data-tab="${tab}"]`).addClass('active');

    $('section.pnl-admin').addClass('d-none');
    $(`section.pnl-admin-${tab}`).removeClass('d-none');

    window.location.hash = `#${tab}`;
}

(function () {
    document.addEventListener('DOMContentLoaded', function () {

        selectActiveTab(window.location.hash);

        $(document).off('click', 'a.pnl-selector').on('click', 'a.pnl-selector', function (e) {
            preventDefaults(e);

            let tab = $(this).data('tab');
            selectActiveTab(tab);
        });

        $(document).off('change', 'input.setting-field,select.setting-field').on('change', 'input.setting-field,select.setting-field', function (e) {
            $(this).attr('data-updated', 'true');
        });

        $(document).off('click', 'button#btnSaveSettings').on('click', 'button#btnSaveSettings', function (e) {
            let updatedFields = $('.setting-field[data-updated="true"]');
            if (updatedFields.length > 0) {
                var settingsList = $.map(updatedFields, function (item) {
                    let element = $(item);
                    return { key: element.data('setting-name'), value: element.val() };
                });

                $.ajax({
                    url: '/Admin/UpdateSettings',
                    method: 'PUT',
                    data: { model: settingsList }
                })
                    .done(data => {
                        if (data.success === true) {
                            displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Success'));
                        } else if (data.message) {
                            displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'), [data.message]);
                        } else {
                            displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'));
                        }
                    })
                    .fail((xhr, error) => {
                        displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'), [error]);
                    });
            } else {
                displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_No_Change'));
            }
        });

        $(document).off('click', 'button.btnReviewApprove').on('click', 'button.btnReviewApprove', function (e) {
            preventDefaults(e);
            reviewPhoto($(this), 1);
        });

        $(document).off('click', 'button.btnReviewReject').on('click', 'button.btnReviewReject', function (e) {
            preventDefaults(e);
            reviewPhoto($(this), 2);
        });

        $(document).off('click', 'i.btnAddUser').on('click', 'i.btnAddUser', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let passwordValidation = `<ul class="password-validator" data-input="input#popup-modal-field-user-password"> \
                    <li class="lbl-lower">${localization.translate('Password_Validation_Lower')}</li> \
                    <li class="lbl-upper">${localization.translate('Password_Validation_Upper')}</li> \
                    <li class="lbl-number">${localization.translate('Password_Validation_Numbers')}</li> \
                    <li class="lbl-special">${localization.translate('Password_Validation_Special')}</li> \
                    <li class="lbl-length">${localization.translate('Password_Validation_Length')}</li> \
                    <li class="lbl-confirm visually-hidden">${localization.translate('Password_Validation_Confirm')}</li> \
                </ul><script>initPasswordValidation();</script>`;

            displayPopup({
                Title: localization.translate('User_Create'),
                Fields: [{
                    Id: 'user-name',
                    Name: localization.translate('User_Name'),
                    Hint: localization.translate('User_Name_Hint')
                },
                {
                    Id: 'user-email',
                    Name: localization.translate('User_Email'),
                    Hint: localization.translate('User_Email_Hint')
                },
                {
                    Id: 'user-password',
                    Name: localization.translate('User_Password'),
                    Hint: localization.translate('User_Password_Hint'),
                    Type: "password"
                },
                {
                    Id: 'user-cpassword',
                    Name: localization.translate('User_Confirm_Password'),
                    Hint: localization.translate('User_Confirm_Password_Hint'),
                    Type: "password",
                    Class: 'confirm-password'
                }],
                FooterHtml: passwordValidation,
                Buttons: [{
                    Text: localization.translate('Add'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let username = $('#popup-modal-field-user-name').val();
                        if (username == undefined || username.length == 0) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Missing_Name'));
                            return;
                        }

                        const usernameRegex = /^[a-zA-Z0-9\-\s-_~]+$/;
                        if (!usernameRegex.test(username)) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Name'));
                            return;
                        }

                        let email = $('#popup-modal-field-user-email').val();
                        const emailRegex = /^((?!\.)[\w\-_.]*[^.])(@[\w\-_]+)(\.\w+(\.\w+)?[^.\W])$/;
                        if (email != undefined && email.length > 0 && !emailRegex.test(email)) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Email'));
                            return;
                        }

                        let password = $('#popup-modal-field-user-password').val();
                        if (password == undefined || password.length < 8) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Password'));
                            return;
                        }

                        let cpassword = $('#popup-modal-field-user-cpassword').val();
                        if (password !== cpassword) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_CPassword'));
                            return;
                        }

                        $.ajax({
                            url: '/Admin/AddUser',
                            method: 'POST',
                            data: { Username: username, Email: email, Password: password, CPassword: cpassword }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateUsersList();
                                    displayMessage(localization.translate('User_Create'), localization.translate('User_Create_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('User_Create'), localization.translate('User_Create_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('User_Create'), localization.translate('User_Create_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('User_Create'), localization.translate('User_Create_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnAddGallery').on('click', 'i.btnAddGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: localization.translate('Gallery_Create'),
                Fields: [{
                    Id: 'gallery-name',
                    Name: localization.translate('Gallery_Name'),
                    Hint: localization.translate('Gallery_Name_Hint')
                }, {
                    Id: 'gallery-key',
                    Name: localization.translate('Gallery_Secret_Key'),
                    Hint: localization.translate('Gallery_Secret_Key_Hint')
                }],
                Buttons: [{
                    Text: localization.translate('Create'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let name = $('#popup-modal-field-gallery-name').val();
                        if (name == undefined || name.length == 0) {
                            displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Missing_Name'));
                            return;
                        }

                        const regex = /^[a-zA-Z0-9\-\s-_~]+$/;
                        if (!regex.test(name)) {
                            displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Invalid_Name'));
                            return;
                        }

                        let key = $('#popup-modal-field-gallery-key').val();

                        $.ajax({
                            url: '/Admin/AddGallery',
                            method: 'POST',
                            data: { Id: 0, Name: name, SecretKey: key }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateGalleryList();
                                    displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Create_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Create_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Create_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Create_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnBulkReview').on('click', 'i.btnBulkReview', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            } 

            displayPopup({
                Title: localization.translate('Bulk_Review'),
                Message: localization.translate('Bulk_Review_Message'),
                Buttons: [{
                    Text: localization.translate('Approve'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));
                        
                        $.ajax({
                            url: '/Admin/BulkReview',
                            method: 'POST',
                            data: { action: 1 }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    hideLoader();
                                } else if (data.message) {
                                    displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Approve_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Approve_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Approve_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Reject'),
                        Class: 'btn-danger',
                        Callback: function () {
                            displayLoader(localization.translate('Loading'));

                            $.ajax({
                                url: '/Admin/BulkReview',
                                method: 'POST',
                                data: { action: 2 }
                            })
                                .done(data => {
                                    if (data.success === true) {
                                        updatePage();
                                        hideLoader();
                                    } else if (data.message) {
                                        displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Reject_Failed'), [data.message]);
                                    } else {
                                        displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Reject_Failed'));
                                    }
                                })
                                .fail((xhr, error) => {
                                    displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Reject_Failed'), [error]);
                                });
                        }
                    }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnImport').on('click', 'i.btnImport', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: localization.translate('Import_Data'),
                Fields: [{
                    Id: 'import-file',
                    Name: localization.translate('Import_Data_Backup_File'),
                    Type: 'File',
                    Hint: localization.translate('Import_Data_Backup_Hint'),
                    Accept: '.zip'
                }],
                Buttons: [{
                    Text: localization.translate('Import'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        var files = $('#popup-modal-field-import-file')[0].files;
                        if (files == undefined || files.length == 0) {
                            displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Select_File'));
                            return;
                        }

                        var data = new FormData();
                        data.append('file-0', files[0]);

                        $.ajax({
                            url: '/Admin/ImportBackup',
                            method: 'POST',
                            data: data,
                            contentType: false,
                            processData: false
                        })
                            .done(data => {
                                if (data.success === true) {
                                    displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Success'));
                                    window.location.reload();
                                } else if (data.message) {
                                    displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnExport').on('click', 'i.btnExport', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: localization.translate('Export_Data'),
                //Message: localization.translate('Export_Data_Message'),
                Fields: [{
                    Id: 'database',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Database'
                }, {
                    Id: 'uploads',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Uploads'
                }, {
                    Id: 'thumbnails',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Thumbnails'
                }, {
                    Id: 'logos',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Logos'
                }, {
                    Id: 'banners',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Banners'
                }, {
                    Id: 'custom-resources',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Custom Resources'
                }],
                Buttons: [{
                    Text: localization.translate('Export'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        $.ajax({
                            url: '/Admin/ExportBackup',
                            method: 'POST',
                            data: {
                                Database: $('#popup-modal-field-database').is(':checked'),
                                Uploads: $('#popup-modal-field-uploads').is(':checked'),
                                Thumbnails: $('#popup-modal-field-thumbnails').is(':checked'),
                                Logos: $('#popup-modal-field-logos').is(':checked'),
                                Banners: $('#popup-modal-field-banners').is(':checked'),
                                CustomResources: $('#popup-modal-field-custom-resources').is(':checked')
                            }
                        })
                            .done(data => {
                                hideLoader();

                                if (data.success === true && data.filename) {
                                    window.location.href = data.filename;
                                } else if (data.message) {
                                    displayMessage(localization.translate('Export_Data'), localization.translate('Export_Data_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Export_Data'), localization.translate('Export_Data_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Export_Data'), localization.translate('Export_Data_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnWipeAllGalleries').on('click', 'i.btnWipeAllGalleries', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: localization.translate('Wipe_Data'),
                Message: localization.translate('Wipe_Data_Message'),
                Buttons: [{
                    Text: localization.translate('Wipe'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        $.ajax({
                            url: '/Admin/WipeAllGalleries',
                            method: 'DELETE'
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('Wipe_Data'), localization.translate('Wipe_Data_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Wipe_Data'), localization.translate('Wipe_Data_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Wipe_Data'), localization.translate('Wipe_Data_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Wipe_Data'), localization.translate('Wipe_Data_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnWipe2FA').on('click', 'i.btnWipe2FA', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('2FA_Setup'),
                Message: localization.translate('2FA_Wipe_Message', { name: row.data('user-name') }),
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Wipe'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('2FA_Setup'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Admin/ResetMultifactorAuthForUser',
                            method: 'DELETE',
                            data: { userId: id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('2FA_Setup'), localization.translate('2FA_Set_Successfully'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('2FA_Setup'), localization.translate('2FA_Set_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('2FA_Setup'), localization.translate('2FA_Set_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('2FA_Setup'), localization.translate('2FA_Set_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnDeleteUser').on('click', 'i.btnDeleteUser', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('User_Delete'),
                Message: localization.translate('User_Delete_Message', { name: row.data('user-name') }),
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Delete'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('User_Delete'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Admin/DeleteUser',
                            method: 'DELETE',
                            data: { id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('User_Delete'), localization.translate('User_Delete_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('User_Delete'), localization.translate('User_Delete_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('User_Delete'), localization.translate('User_Delete_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('User_Delete'), localization.translate('User_Delete_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnOpenGallery').on('click', 'i.btnOpenGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            window.open($(this).data('url'), $(this).data('target'));
        });

        $(document).off('click', 'i.btnDownloadGallery').on('click', 'i.btnDownloadGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayLoader(localization.translate('Loading'));

            let row = $(this).closest('tr');
            let id = row.data('gallery-id');

            $.ajax({
                url: '/Gallery/DownloadGallery',
                method: 'POST',
                data: { Id: id }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true && data.filename) {
                        window.location.href = data.filename;
                    } else if (data.message) {
                        displayMessage(localization.translate('Download'), localization.translate('Download_Failed'), [data.message]);
                    } else {
                        displayMessage(localization.translate('Download'), localization.translate('Download_Failed'));
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(localization.translate('Download'), localization.translate('Download_Failed'), [error]);
                });
        });

        $(document).off('click', 'i.btnEditGallery').on('click', 'i.btnEditGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('Gallery_Edit'),
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }, {
                    Id: 'gallery-name',
                    Name: localization.translate('Gallery_Name'),
                    Value: row.data('gallery-name'),
                    Hint: localization.translate('Gallery_Name_Hint')
                }, {
                    Id: 'gallery-key',
                    Name: localization.translate('Gallery_Secret_Key'),
                    Value: row.data('gallery-key'),
                    Hint: localization.translate('Gallery_Secret_Key_Hint')
                }],
                Buttons: [{
                    Text: localization.translate('Update'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Missing_Id'));
                            return;
                        }

                        let name = $('#popup-modal-field-gallery-name').val();
                        if (name == undefined || name.length == 0) {
                            displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Missing_Name'));
                            return;
                        }

                        let key = $('#popup-modal-field-gallery-key').val();

                        $.ajax({
                            url: '/Admin/EditGallery',
                            method: 'PUT',
                            data: { Id: id, Name: name, SecretKey: key }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateGalleryList();
                                    displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Edit_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Edit_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Edit_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Edit_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnEditUser').on('click', 'i.btnEditUser', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let passwordValidation = `<ul class="password-validator" data-input="input#popup-modal-field-user-password"> \
                    <li class="lbl-lower">Lower case letters</li> \
                    <li class="lbl-upper">Upper case letter</li> \
                    <li class="lbl-number">Numbers</li> \
                    <li class="lbl-special">Special characters</li> \
                    <li class="lbl-length">At least 8 characters</li> \
                    <li class="lbl-confirm visually-hidden">Confirm password matches</li> \
                </ul><script>initPasswordValidation();</script>`;

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('User_Edit'),
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }, {
                    Id: 'user-name',
                    Name: localization.translate('User_Name'),
                    Value: row.data('user-name'),
                    Hint: localization.translate('User_Name_Hint'),
                    Disabled: true
                }, {
                    Id: 'user-email',
                    Name: localization.translate('User_Email'),
                    Value: row.data('user-email'),
                    Hint: localization.translate('User_Email_Hint')
                }, {
                    Id: 'user-password',
                    Name: localization.translate('User_Password'),
                    Value: row.data('user-password'),
                    Hint: localization.translate('User_Password_Hint'),
                    Type: 'password'
                }, {
                    Id: 'user-cpassword',
                    Name: localization.translate('User_Confirm_Password'),
                    Value: row.data('user-cpassword'),
                    Hint: localization.translate('User_Confirm_Password_Hint'),
                    Type: 'password',
                    Class: 'confirm-password'
                }],
                FooterHtml: passwordValidation,
                Buttons: [{
                    Text: localization.translate('Update'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('User_Edit'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        let email = $('#popup-modal-field-user-email').val();
                        const emailRegex = /^((?!\.)[\w\-_.]*[^.])(@[\w\-_]+)(\.\w+(\.\w+)?[^.\W])$/;
                        if (email != undefined && email.length > 0 && !emailRegex.test(email)) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Email'));
                            return;
                        }

                        let password = $('#popup-modal-field-user-password').val();
                        if (password == undefined || password.length < 8) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Password'));
                            return;
                        }

                        let cpassword = $('#popup-modal-field-user-cpassword').val();
                        if (password !== cpassword) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_CPassword'));
                            return;
                        }

                        $.ajax({
                            url: '/Admin/EditUser',
                            method: 'PUT',
                            data: { Id: id, Email: email, Password: password, CPassword: cpassword }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateUsersList();
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnWipeGallery').on('click', 'i.btnWipeGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('Gallery_Wipe'),
                Message: localization.translate('Gallery_Wipe_Message', { name: row.data('gallery-name') }),
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Wipe'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Admin/WipeGallery',
                            method: 'DELETE',
                            data: { id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Wipe_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Wipe_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Wipe_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Wipe_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnDeleteGallery').on('click', 'i.btnDeleteGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('Gallery_Delete'),
                Message: localization.translate('Gallery_Delete_Message', { name: row.data('gallery-name') }),
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Delete'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Admin/DeleteGallery',
                            method: 'DELETE',
                            data: { id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Delete_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Delete_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Delete_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Delete_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

    });
})();