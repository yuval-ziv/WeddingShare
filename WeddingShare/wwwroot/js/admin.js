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

            displayPopup('add-gallery-modal');
        });

        $(document).off('click', 'i.btnOpenGallery').on('click', 'i.btnOpenGallery', function (e) {
            preventDefaults(e);
            window.open($(this).data('url'), '_blank');
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

            let row = $(this).closest('tr');

            $('#edit-gallery-modal #gallery-id').val(row.data('gallery-id'));
            $('#edit-gallery-modal #gallery-name').val(row.data('gallery-name'));
            $('#edit-gallery-modal #gallery-key').val(row.data('gallery-key'));

            displayPopup('edit-gallery-modal');
        });

        $(document).off('click', 'i.btnDeleteGallery').on('click', 'i.btnDeleteGallery', function (e) {
            preventDefaults(e);

            let row = $(this).closest('tr');

            $('#delete-gallery-modal #gallery-id').val(row.data('gallery-id'));
            $('#delete-gallery-modal #gallery-name').val(row.data('gallery-name'));

            displayPopup('delete-gallery-modal');
        });

        $(document).off('click', '#add-gallery-modal .btnCreate').on('click', '#add-gallery-modal .btnCreate', function (e) {
            preventDefaults(e);

            hidePopup('add-gallery-modal');
            displayLoader('Loading...');

            let name = $('#add-gallery-modal #gallery-name').val();
            let key = $('#add-gallery-modal #gallery-key').val();

            $.ajax({
                url: '/Admin/AddGallery',
                method: 'POST',
                data: { Id: 0, Name: name, SecretKey: key }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true) {
                        displayMessage(`Create`, `Successfully created gallery`);
                    } else if (data.message) {
                        displayMessage(`Create`, `Create failed`, [data.message]);
                    } else {
                        displayMessage(`Create`, `Failed to create gallery`);
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(`Create`, `Create failed`, [error]);
                });
        });

        $(document).off('click', '#edit-gallery-modal .btnUpdate').on('click', '#edit-gallery-modal .btnUpdate', function (e) {
            preventDefaults(e);

            hidePopup('edit-gallery-modal');
            displayLoader('Loading...');

            let id = $('#edit-gallery-modal #gallery-id').val();
            let name = $('#edit-gallery-modal #gallery-name').val();
            let key = $('#edit-gallery-modal #gallery-key').val();

            $.ajax({
                url: '/Admin/EditGallery',
                method: 'PUT',
                data: { Id: id, Name: name, SecretKey: key }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true) {
                        $(`tr[data-gallery-id=${id}] .gallery-name`).text(name);
                        displayMessage(`Update`, `Successfully updated gallery`);
                    } else if (data.message) {
                        displayMessage(`Update`, `Update failed`, [data.message]);
                    } else {
                        displayMessage(`Update`, `Failed to update gallery`);
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(`Update`, `Update failed`, [error]);
                });
        });

        $(document).off('click', '#delete-gallery-modal .btnDelete').on('click', '#delete-gallery-modal .btnDelete', function (e) {
            preventDefaults(e);

            hidePopup('delete-gallery-modal');
            displayLoader('Loading...');

            let id = $('#delete-gallery-modal #gallery-id').val();

            $.ajax({
                url: '/Admin/DeleteGallery',
                method: 'DELETE',
                data: { id }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true) {
                        $(`tr[data-gallery-id=${id}]`).remove();
                        displayMessage(`Delete`, `Successfully deleted gallery`);
                    } else if (data.message) {
                        displayMessage(`Delete`, `Delete failed`, [data.message]);
                    } else {
                        displayMessage(`Delete`, `Failed to delete gallery`);
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(`Delete`, `Delete failed`, [error]);
                });
        });

    });
})();