function reviewPhoto(element, action) {
    var id = element.parent('.btn-group').data('id');
    if (!id) {
        displayMessage(`Review`, `Could not find item Id`);
        return;
    }

    displayLoader('Loading...');

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
                displayMessage(`Review`, `Review failed`, [data.message]);
            }
        })
        .fail((xhr, error) => {
            hideLoader();
            displayMessage(`Review`, `Review failed`, [error]);
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
                Title: 'Create Gallery',
                Fields: [{
                    Id: 'gallery-name',
                    Name: 'Gallery Name',
                    Hint: 'Please enter a new gallery name'
                }, {
                    Id: 'gallery-key',
                    Name: 'Secret Key',
                    Hint: 'Please enter a new secret key'
                }],
                Buttons: [{
                    Text: 'Create',
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader('Loading...');

                        let name = $('#popup-modal-field-gallery-name').val();
                        if (name == undefined || name.length == 0) {
                            displayMessage(`Create Gallery`, `Gallery name cannot be empty`);
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
                                    displayMessage(`Create Gallery`, `Successfully created gallery`);
                                } else if (data.message) {
                                    displayMessage(`Create Gallery`, `Create failed`, [data.message]);
                                } else {
                                    displayMessage(`Create Gallery`, `Failed to create gallery`);
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(`Create Gallery`, `Create failed`, [error]);
                            });
                    }
                }, {
                    Text: 'Close'
                }]
            });
        });

        $(document).off('click', 'i.btnImport').on('click', 'i.btnImport', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: 'Import Data',
                Fields: [{
                    Id: 'import-file',
                    Name: 'Backup File',
                    Type: 'File',
                    Hint: 'Please select a WeddingShare backup archive',
                    Accept: '.zip'
                }],
                Buttons: [{
                    Text: 'Import',
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader('Loading...');

                        var files = $('#popup-modal-field-import-file')[0].files;
                        if (files == undefined || files.length == 0) {
                            displayMessage(`Import Data`, `Please select an import file`);
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
                                    displayMessage(`Import Data`, `Successfully imported data`);
                                    window.location.reload();
                                } else if (data.message) {
                                    displayMessage(`Import Data`, `Import failed`, [data.message]);
                                } else {
                                    displayMessage(`Import Data`, `Failed to import data`);
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(`Import Data`, `Import failed`, [error]);
                            });
                    }
                }, {
                    Text: 'Close'
                }]
            });
        });

        $(document).off('click', 'i.btnExport').on('click', 'i.btnExport', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: 'Export Data',
                Message: 'Are you sure you want to continue?',
                Buttons: [{
                    Text: 'Export',
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader('Loading...');

                        $.ajax({
                            url: '/Admin/ExportBackup',
                            method: 'GET'
                        })
                            .done(data => {
                                hideLoader();

                                if (data.success === true) {
                                    var s = window.atob(data.content);
                                    var bytes = new Uint8Array(s.length);
                                    for (var i = 0; i < s.length; i++) {
                                        bytes[i] = s.charCodeAt(i);
                                    }

                                    var blob = new Blob([bytes], { type: "application/octetstream" });

                                    var isIE = false || !!document.documentMode;
                                    if (isIE) {
                                        window.navigator.msSaveBlob(blob, data.filename);
                                    } else {
                                        var url = window.URL || window.webkitURL;
                                        link = url.createObjectURL(blob);
                                        var a = $("<a />");
                                        a.attr("download", data.filename);
                                        a.attr("href", link);
                                        $("body").append(a);
                                        a[0].click();
                                        $("body").remove(a);
                                    }
                                } else if (data.message) {
                                    displayMessage(`Export Data`, `Export failed`, [data.message]);
                                } else {
                                    displayMessage(`Export Data`, `Failed to export data`);
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(`Export Data`, `Export failed`, [error]);
                            });
                    }
                }, {
                    Text: 'Close'
                }]
            });
        });

        $(document).off('click', 'i.btnWipeAllGalleries').on('click', 'i.btnWipeAllGalleries', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: 'Wipe Data',
                Message: 'Are you sure you want to wipe all data?',
                Buttons: [{
                    Text: 'Wipe',
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader('Loading...');

                        $.ajax({
                            url: '/Admin/WipeAllGalleries',
                            method: 'DELETE'
                        })
                            .done(data => {
                                if (data.success === true) {
                                    displayMessage(`Wipe Data`, `Successfully wiped data`);
                                } else if (data.message) {
                                    displayMessage(`Wipe Data`, `Wipe failed`, [data.message]);
                                } else {
                                    displayMessage(`Wipe Data`, `Failed to wipe data`);
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(`Wipe Data`, `Wipe failed`, [error]);
                            });
                    }
                }, {
                    Text: 'Close'
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

            displayLoader('Loading...');

            let row = $(this).closest('tr');
            let id = row.data('gallery-id');

            $.ajax({
                url: '/Admin/DownloadGallery',
                method: 'POST',
                data: { Id: id }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true) {
                        var s = window.atob(data.content);
                        var bytes = new Uint8Array(s.length);
                        for (var i = 0; i < s.length; i++) {
                            bytes[i] = s.charCodeAt(i);
                        }

                        var blob = new Blob([bytes], { type: "application/octetstream" });

                        var isIE = false || !!document.documentMode;
                        if (isIE) {
                            window.navigator.msSaveBlob(blob, data.filename);
                        } else {
                            var url = window.URL || window.webkitURL;
                            link = url.createObjectURL(blob);
                            var a = $("<a />");
                            a.attr("download", data.filename);
                            a.attr("href", link);
                            $("body").append(a);
                            a[0].click();
                            $("body").remove(a);
                        }
                    } else if (data.message) {
                        displayMessage(`Download`, `Download failed`, [data.message]);
                    } else {
                        displayMessage(`Download`, `Failed to download gallery`);
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(`Download`, `Download failed`, [error]);
                });
        });

        $(document).off('click', 'i.btnEditGallery').on('click', 'i.btnEditGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: 'Edit Gallery',
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }, {
                    Id: 'gallery-name',
                    Name: 'Gallery Name',
                    Value: row.data('gallery-name'),
                    Hint: 'Please enter a new gallery name'
                }, {
                    Id: 'gallery-key',
                    Name: 'Secret Key',
                    Value: row.data('gallery-key'),
                    Hint: 'Please enter a new secret key'
                }],
                Buttons: [{
                    Text: 'Update',
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader('Loading...');

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(`Edit Gallery`, `Gallery id cannot be empty`);
                            return;
                        }

                        let name = $('#popup-modal-field-gallery-name').val();
                        if (name == undefined || name.length == 0) {
                            displayMessage(`Edit Gallery`, `Gallery name cannot be empty`);
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
                                    displayMessage(`Edit Gallery`, `Successfully updated gallery`);
                                } else if (data.message) {
                                    displayMessage(`Edit Gallery`, `Update failed`, [data.message]);
                                } else {
                                    displayMessage(`Edit Gallery`, `Failed to update gallery`);
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(`Edit Gallery`, `Update failed`, [error]);
                            });
                    }
                }, {
                    Text: 'Close'
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
                Title: 'Wipe Gallery',
                Message: `Are you sure you want to wipe gallery '${row.data('gallery-name') }'?`,
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: 'Wipe',
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader('Loading...');

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(`Wipe Gallery`, `Gallery id cannot be empty`);
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
                                    displayMessage(`Wipe Gallery`, `Successfully wiped gallery`);
                                } else if (data.message) {
                                    displayMessage(`Wipe Gallery`, `Wipe failed`, [data.message]);
                                } else {
                                    displayMessage(`Wipe Gallery`, `Failed to wipe gallery`);
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(`Wipe Gallery`, `Wipe failed`, [error]);
                            });
                    }
                }, {
                    Text: 'Close'
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
                Title: 'Delete Gallery',
                Message: `Are you sure you want to delete gallery '${row.data('gallery-name')}'?`,
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: 'Delete',
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader('Loading...');

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(`Delete Gallery`, `Gallery id cannot be empty`);
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
                                    displayMessage(`Delete Gallery`, `Successfully deleted gallery`);
                                } else if (data.message) {
                                    displayMessage(`Delete Gallery`, `Delete failed`, [data.message]);
                                } else {
                                    displayMessage(`Delete Gallery`, `Failed to delete gallery`);
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(`Delete Gallery`, `Delete failed`, [error]);
                            });
                    }
                }, {
                    Text: 'Close'
                }]
            });
        });

    });
})();