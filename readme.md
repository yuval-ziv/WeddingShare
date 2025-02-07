# WeddingShare

 <br />![Banner](https://github.com/Cirx08/WeddingShare/blob/main/banner.png?raw=true)

## Support

Thank you to everyone that supports this project. For anyone that hasn't yet I would be grateful if you would show some support by "buying me a coffee" through the link below. Weddings are expensive and all proceeds from this project will be going towards paying off my wedding bills.

- BuyMeACoffee - https://buymeacoffee.com/cirx08

## About

WeddingShare is a very basic site with only one goal. It provides you and your guests a way to share memories of and leading up to the big day. Simply provide your guests with a link to a gallery either via a Url or even better by printing out the provided QR code and dropping it on your guests' dinner tables. Doing so will allow them to view your journey up to this point such as dress/suit shopping, viewing the venue, doing the food tasting or cake shopping, etc. 

You are not limited to a single gallery. You can generate multiple galleries all with their own sharable links. At this stage galleries are a bit unsecure, meaning anyone with the link has access to view and share images so I recommend keeping your share links private. To combat strangers gaining access to your galleries you can provide a secret key during setup but be advised this is a deterrent to make guessing Urls slightly harder and not an actual security catch all. 

## Disclaimer

Warning. This is open-source software (GPL-V3), and while we make a best effort to ensure releases are stable and bug-free,
there are no warranties. Use at your own risk.

## Notes

Not all image formats are supported in browsers so although you may be able to add them via the ALLOWED_FILE_TYPES environment variable they may not be supported. One such format is Apples .heic format. It is specific to Apple devices and due to its licensing, a lot of browsers have not implemented it.

# Basic

:::warning Media Formats
Not all image formats are supported in browsers so although you may be able to add them via the `ALLOWED_FILE_TYPES` environment variable they may not be supported. One such format is Apples `.heic` format. It is specific to Apple devices and due to its licensing, a lot of browsers have not implemented it.
:::

| Name                                       | Value                                        |
| ------------------------------------------ | -------------------------------------------- |
| TITLE                                      | WeddingShare                                 |
| LOGO                                       | https://someurl/someimage.png                |
| BASE_URL                                   | www.wedding-share.com                        |
| LANGUAGE                                   | en-GB                                        |
| FORCE_HTTPS                                | false                                        |
| SINGLE_GALLERY_MODE                        | false                                        |
| HOME_LINK                                  | true                                         |
| GUEST_GALLERY_CREATION                     | false                                        |
| HIDE_KEY_FROM_QR_CODE                      | false                                        |
| LINKS_OPEN_NEW_TAB                         | false                                        |
| THUMBNAIL_SIZE                             | 720                                          |
| EMAIL_REPORT                               | true                                         |

### Gallery

:::tip Gallery Overrides
All settings in the table below can have a gallery specific override by appending the gallery name to the end of the key. For example if the environment variable `REQUIRE_REVIEW_PROPOSAL` is specified it will override the value specified using the `REQUIRE_REVIEW` environment variable.
:::

| Name                               | Value                                        |
| ---------------------------------- | -------------------------------------------- |
| TITLE                              | WeddingShare                                 |
| LOGO                               | https://someurl/someimage.png                |
| GALLERY_QUOTE                      | (optional)                                   |
| GALLERY_SECRET_KEY                 | (optional)                                   |
| GALLERY_COLUMNS                    | 4 (1, 2, 3, 4, 6, 12)                        |
| GALLERY_ITEMS_PER_PAGE             | 50                                           |
| GALLERY_FULL_WIDTH                 | false                                        |
| GALLERY_RETAIN_REJECTED_ITEMS      | false                                        |
| GALLERY_UPLOAD                     | false                                        |
| GALLERY_DOWNLOAD                   | false                                        |
| GALLERY_REQUIRE_REVIEW             | true                                         |
| GALLERY_REVIEW_COUNTER             | true                                         |
| GALLERY_PREVENT_DUPLICATES         | true                                         |
| GALLERY_QR_CODE                    | true                                         |
| GALLERY_IDLE_REFRESH_MINS          | 5 (0 = disable)                              |
| GALLERY_MAX_SIZE_MB                | 1024                                         |
| GALLERY_MAX_FILE_SIZE_MB           | 10                                           |
| GALLERY_ALLOWED_FILE_TYPES         | .jpg,.jpeg,.png,.mp4,.mov                    |
| GALLERY_DEFAULT_VIEW               | 0 (Default), 1 (Presentation), 2 (Slideshow) |
| GALLERY_UPLOAD_PERIOD              | "2025-01-29 23:59" or "2025-01-01 00:00 / 2025-01-03 23:59" or "2025-01-01 00:00 / 2025-01-01 23:59, 2025-01-03 00:00 / 2025-01-03 23:59" |

# Account

| Name                           | Value                                        |
| ------------------------------ | -------------------------------------------- |
| ACCOUNT_ADMIN_USERNAME         | admin                                        |
| ACCOUNT_ADMIN_PASSWORD         | admin                                        |
| ACCOUNT_ADMIN_LOG_PASSWORD     | true                                         |
| ACCOUNT_SHOW_PROFILE_ICON      | true                                         |
| ACCOUNT_LOCKOUT_ATTEMPTS       | 5                                            |
| ACCOUNT_LOCKOUT_MINS           | 60                                           |

# Identity Check

| Name                                       | Value                                        |
| ------------------------------------------ | -------------------------------------------- |
| IDENTITY_CHECK_ENABLED                     | true                                         |
| IDENTITY_CHECK_SHOW_ON_PAGE_LOAD           | true                                         |
| IDENTITY_CHECK_REQUIRE_IDENTITY_FOR_UPLOAD | false                                        |

# Slideshow

| Name                           | Value                                        |
| ------------------------------ | -------------------------------------------- |
| SLIDESHOW_INTERVAL             | 10 (seconds)                                 |
| SLIDESHOW_FADE                 | 500 (milliseconds)                           |
| SLIDESHOW_LIMIT                | (optional)                                   |
| SLIDESHOW_INCLUDE_SHARE_SLIDE  | true                                         |

# Background Services

| Name                           | Value                                        |
| ------------------------------ | -------------------------------------------- |
| SCHEDULES_DIRECTORY_SCANNER    | */30 * * * * (cron)                          |
| SCHEDULES_EMAIL_REPORT         | 0 0 * * * (cron)                             |
| SCHEDULES_CLEANUP              | 0 4 * * * (cron)                             |

# Alerts

| Name                           | Value                                        |
| ------------------------------ | -------------------------------------------- |
| ALERTS_FAILED_LOGIN            | true                                         |
| ALERTS_ACCOUNT_LOCKOUT         | true                                         |
| ALERTS_DESTRUCTIVE_ACTION      | true                                         |
| ALERTS_PENDING_REVIEW          | true                                         |

# STMP

| Name                           | Value                                        |
| ------------------------------ | -------------------------------------------- |
| SMTP_ENABLED                   | false                                        |
| SMTP_RECIPIENT                 | (required)                                   |
| SMTP_HOST                      | (required)                                   |
| SMTP_PORT                      | 587                                          |
| SMTP_USERNAME                  | (required)                                   |
| SMTP_PASSWORD                  | (required)                                   |
| SMTP_FROM                      | (required)                                   |
| SMTP_DISPLAYNAME               | WeddingShare                                 |
| SMTP_USE_SSL                   | true                                         |

# NTFY

| Name                           | Value                                        |
| ------------------------------ | -------------------------------------------- |
| NTFY_ENABLED                   | false                                        |
| NTFY_ENDPOINT                  | (required)                                   |
| NTFY_TOKEN                     | (required)                                   |
| NTFY_TOPIC                     | WeddingShare                                 |
| NTFY_PRIORITY                  | 4                                            |

# Gotify

| Name                           | Value                                        |
| ------------------------------ | -------------------------------------------- |
| GOTIFY_ENABLED                 | false                                        |
| GOTIFY_ENDPOINT                | (required)                                   |
| GOTIFY_TOKEN                   | (required)                                   |
| GOTIFY_PRIORITY                | 4                                            |

# Encryption

| Name                            | Value                                        |
| ------------------------------- | -------------------------------------------- |
| ENCRYPTION_KEY                  | ChangeMe                                     |
| ENCRYPTION_SALT                 | ChangeMe                                     |
| ENCRYPTION_ITERATIONS           | 1000                                         |

# 2FA

| Name                            | Value                                        |
| ------------------------------- | -------------------------------------------- |
| 2FA_RESET_TO_DEFAULT            | false                                        |

:::tip Reset 2FA
If for any reason your 2FA no longer works correctly you can reset it by setting the `2FA_RESET_TO_DEFAULT` environment variable to `true` and restarting the container. Just be sure to remove it or set it back to `false` once done or it will reset again next restart.
:::

# Request Headers

| Name                            | Value                                        |
| ------------------------------- | -------------------------------------------- |
| HEADERS_ENABLED                 | true                                         |
| HEADERS_X_FRAME_OPTIONS         | SAMEORIGIN                                   |
| HEADERS_X_CONTENT_TYPE_OPTIONS  | nosniff                                      |
| HEADERS_CSP_HEADER              | (optional)                                   |

# Themes

| Name                            | Value                                        |
| ------------------------------- | -------------------------------------------- |
| THEMES_ENABLED                  | true                                         |
| THEMES_DEFAULT                  | default (default, dark)                      |

## Docker Run

```
docker run --name WeddingShare -h wedding-share -p 8080:5000 -v /var/lib/docker/volumes/wedding-share-config/_data:/app/config:rw -v /var/lib/docker/volumes/wedding-share-thumbnails/_data:/app/wwwroot/thumbnails:rw -v /var/lib/docker/volumes/wedding-share-uploads/_data:/app/wwwroot/uploads:rw --restart always cirx08/wedding_share:latest
```

## Docker Compose

```
services:
  wedding-share:
    container_name: WeddingShare
    image: cirx08/wedding_share:latest
    ports:
      - '${HTTP_PORT:-8080}:5000/tcp'
    environment:
      TITLE: 'WeddingShare'
      LOGO: 'Url'
      GALLERY_COLUMNS: 4
      GALLERY_ALLOWED_FILE_TYPES: '.jpg,.jpeg,.png'
      GALLERY_MAX_FILE_SIZE_MB: 10
      GALLERY_SECRET_KEY: 'password'
    volumes:
      - data-volume-config:/app/config
      - data-volume-thumbnails:/app/wwwroot/thumbnails
      - data-volume-uploads:/app/wwwroot/uploads
    network_mode: bridge
    hostname: wedding-share
    restart: always

volumes:
  data-volume-config:
    name: WeddingShare-Config
  data-volume-thumbnails:
    name: WeddingShare-Thumbnails
  data-volume-uploads:
    name: WeddingShare-Uploads
```

## Links
- Documentation - https://docs.wedding-share.org
- GitHub - https://github.com/Cirx08/WeddingShare
- DockerHub - https://hub.docker.com/r/cirx08/wedding_share
- BuyMeACoffee - https://buymeacoffee.com/cirx08

## Screenshots

### Desktop

![Homepage](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Homepage.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-FullWidth.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Presentation.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Slideshow.png?raw=true)

![Admin Area](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Admin.png?raw=true)

### Mobile

![Homepage](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Homepage-Mobile.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Mobile.png?raw=true)

![Admin Area](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Admin-Mobile.png?raw=true)

### Dark Mode

![Homepage](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Homepage-Dark.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Dark.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Presentation-Dark.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Slideshow-Dark.png?raw=true)

![Admin Area](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Admin-Dark.png?raw=true)

![Homepage](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Homepage-Mobile-Dark.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Mobile-Dark.png?raw=true)

![Admin Area](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Admin-Mobile-Dark.png?raw=true)

### Attributions
- [Bride icons created by Freepik - Flaticon](https://www.flaticon.com/free-icon/wedding-couple_703213)