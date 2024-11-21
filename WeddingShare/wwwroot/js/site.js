/* Bootstrap 5 JS included */

('use strict');

// Drag and drop - single or multiple image files
// https://www.smashingmagazine.com/2018/01/drag-drop-file-uploader-vanilla-js/
// https://codepen.io/joezimjs/pen/yPWQbd?editors=1000
(function () {

    'use strict';

    // Four objects of interest: drop zones, input elements, gallery elements, and the files.
    // dataRefs = {files: [image files], input: element ref, gallery: element ref}

    const preventDefaults = event => {
        event.preventDefault();
        event.stopPropagation();
    };

    const triggerSelector = event => {
        const zone = event.target.closest('.upload_drop') || false;
        const input = zone.querySelector('input[type="file"]') || false;
        input.click();
    }

    const highlight = event =>
        event.target.classList.add('highlight');

    const unhighlight = event =>
        event.target.classList.remove('highlight');

    const getInputAndGalleryRefs = element => {
        const zone = element.closest('.upload_drop') || false;
        const gallery = zone.querySelector('.upload_gallery') || false;
        const input = zone.querySelector('input[type="file"]') || false;
        return { input: input, gallery: gallery };
    }

    const handleDrop = event => {
        const dataRefs = getInputAndGalleryRefs(event.target);
        dataRefs.files = event.dataTransfer.files;
        handleFiles(dataRefs);
    }

    const eventHandlers = zone => {

        const dataRefs = getInputAndGalleryRefs(zone);

        if (!dataRefs.input) return;

        // Prevent default drag behaviors
        ;['dragenter', 'dragover', 'dragleave', 'drop'].forEach(event => {
            zone.addEventListener(event, preventDefaults, false);
            document.body.addEventListener(event, preventDefaults, false);
        });

        // Open file browser on drop area click
        ;['click', 'touch'].forEach(event => {
            zone.addEventListener(event, triggerSelector, false);
        });
        // Highlighting drop area when item is dragged over it
        ;['dragenter', 'dragover'].forEach(event => {
            zone.addEventListener(event, highlight, false);
        });
        ;['dragleave', 'drop'].forEach(event => {
            zone.addEventListener(event, unhighlight, false);
        });

        // Handle dropped files
        zone.addEventListener('drop', handleDrop, false);

        // Handle browse selected files
        dataRefs.input.addEventListener('change', event => {
            dataRefs.files = event.target.files;
            handleFiles(dataRefs);
        }, false);

    }

    // Initialise ALL dropzones
    const dropZones = document.querySelectorAll('.upload_drop');
    for (const zone of dropZones) {
        eventHandlers(zone);
    }

    // No 'image/gif' or PDF or webp allowed here, but it's up to your use case.
    // Double checks the input "accept" attribute
    const isImageFile = file => ['image/jpeg', 'image/png'].includes(file.type);

    // Based on: https://flaviocopes.com/how-to-upload-files-fetch/
    const imageUpload = dataRefs => {

        // Multiple source routes, so double check validity
        if (!dataRefs.files || !dataRefs.input) {
            displayMessage(`Upload`, `No files were detected to upload`);
            return;
        }

        const galleryId = dataRefs.input.getAttribute('data-post-gallery-id');
        if (!galleryId) {
            displayMessage(`Upload`, `Invalid gallery Id detected`);
            return;
        }

        const url = dataRefs.input.getAttribute('data-post-url');
        if (!url) {
            displayMessage(`Upload`, `Could not find upload Url`);
            return;
        }

        const secretKey = dataRefs.input.getAttribute('data-post-key');

        const formData = new FormData();
        formData.append('SecretKey', secretKey);
        formData.append('GalleryId', galleryId);
        for (var i = 0; i < dataRefs.files.length; i++) {
            formData.append(dataRefs.files[i].name, dataRefs.files[i]);
        }

        displayLoader('Uploading...');

        fetch(url, {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                hideLoader();

                if (data.success === true) {
                    if (data.requiresReview === true) {
                        displayMessage(`Upload`, `Successfully uploaded ${data.uploaded} photo(s) pending review`, data.errors);
                    } else {
                        displayMessage(`Upload`, `Successfully uploaded ${data.uploaded} photo(s)`, data.errors);
                    }
                } else if (data.message) {
                    displayMessage(`Upload`, `Upload failed`, [data.message]);
                }
            })
            .catch(error => {
                hideLoader();
                displayMessage(`Upload`, `Upload failed`, [error]);
            });
    }

    // Handle both selected and dropped files
    const handleFiles = dataRefs => {

        let files = [...dataRefs.files];

        // Remove unaccepted file types
        files = files.filter(item => {
            if (!isImageFile(item)) {
                console.log('Not an image, ', item.type);
            }
            return isImageFile(item) ? item : null;
        });

        if (!files.length) return;
        dataRefs.files = files;

        imageUpload(dataRefs);
    }

    function reviewPhoto(element, action) {
        var galleryId = element.parent('.btn-group').data('gallery-id');
        if (!galleryId) {
            displayMessage(`Review`, `Could not find gallery Id`);
            return;
        }

        var photoId = element.parent('.btn-group').data('photo-id')
        if (!photoId) {
            displayMessage(`Review`, `Could not find photo Id`);
            return;
        }

        displayLoader('Loading...');

        $.ajax({
            url: '/Admin/ReviewPhoto',
            method: 'POST',
            data: { galleryId, photoId, action }
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

    function displayMessage(title, message, errors) {
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
                }
            })
            .fail((xhr, error) => {
                hideLoader();
                displayMessage(`Login`, `Login failed`, [error]);
            });
    });

    $(document).off('click', 'button.btnReviewApprove').on('click', 'button.btnReviewApprove', function (e) {
        preventDefaults(e);
        reviewPhoto($(this), 1);
    });

    $(document).off('click', 'button.btnReviewReject').on('click', 'button.btnReviewReject', function (e) {
        preventDefaults(e);
        reviewPhoto($(this), 2);
    });

    lightbox.option({
        'disableScrolling': true
    });
})();