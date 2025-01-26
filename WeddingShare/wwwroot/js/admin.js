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

                if ($('.pending-approval').length == 0) {
                    $('#gallery-review').addClass('visually-hidden');
                    $('#no-review-msg').removeClass('visually-hidden');
                } else {
                    $('#no-review-msg').addClass('visually-hidden');
                    $('#gallery-review').removeClass('visually-hidden');
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

(function () {
    document.addEventListener('DOMContentLoaded', function () {

        $(document).off('click', 'button.btnReviewApprove').on('click', 'button.btnReviewApprove', function (e) {
            preventDefaults(e);
            reviewPhoto($(this), 1);
        });

        $(document).off('click', 'button.btnReviewReject').on('click', 'button.btnReviewReject', function (e) {
            preventDefaults(e);
            reviewPhoto($(this), 2);
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
                                    $('.pending-approval').remove();
                                    $('#gallery-review').addClass('visually-hidden');
                                    $('#no-review-msg').removeClass('visually-hidden');
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
                                        $('.pending-approval').remove();
                                        $('#gallery-review').addClass('visually-hidden');
                                        $('#no-review-msg').removeClass('visually-hidden');
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
                Message: localization.translate('Export_Data_Message'),
                Buttons: [{
                    Text: localization.translate('Export'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        $.ajax({
                            url: '/Admin/ExportBackup',
                            method: 'GET'
                        })
                            .done(data => {
                                hideLoader();

                                if (data.success === true && data.filename) {
                                    window.location.href = data.filename;
                                } else if (data.message) {
                                    displayMessage(localization.translate('Export_Data'), localization.translate('Export_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Export_Data'), localization.translate('Export_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Export_Data'), localization.translate('Export_Failed'), [error]);
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
                                    $(`tr[data-gallery-id=${id}] .gallery-name`).text(name);
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
                                    $(`tr[data-gallery-id=${id}] .gallery-name`).text(name);
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
                                    $(`tr[data-gallery-id=${id}]`).remove();
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