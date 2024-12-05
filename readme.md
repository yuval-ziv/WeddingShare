# WeddingShare
 
 <br />![Banner](https://github.com/Cirx08/WeddingShare/blob/main/banner.png?raw=true)

## About

WeddingShare is a very basic site with only one goal. It provides you and your guests a way to share memories of and leading up to the big day. Simply provide your guests with a link to a gallery either via a Url or even better by printing out the provided QR code and dropping it on your guests dinner tables. Doing so will allow them to view your journey up to this point such as dress/suit shopping, viewing the venue, doing the food tasting or cake shopping, etc. 
You are not limited to a single gallery. You can generate multiple gallerys all with their own sharable links. At this stage galleries are a bit unsecure, meaning anyone with the link has acess to view and share images so I recommend keeping your share links private. To combat strangers gaining access to your galleries you can provide a secret key during setup but be advised this is a deterant to make guessing Urls slightly harder and not an actual security catch all. 

## Disclaimer

Warning. This is open source software (GPL-V3), and while we make a best effort to ensure releases are stable and bug-free,
there are no warranties. Use at your own risk.

## Notes

Not all image formats are supported in browsers so although you may be able to add them via the ALLOWED_FILE_TYPES environment variable they may not be supported. One such format is Apples .heic format. It is specific to Apple devices and due to its licensing a lot of browsers have not implemented it.

## Settings

| Name                           | Value                         |
| ------------------------------ | ----------------------------- |
| TITLE                          | WeddingShare                  |
| LOGO                           | https://someurl/someimage.png |
| FORCE_HTTPS                    | false                         |
| GALLERY_COLUMNS                | 4                             |
| ALLOWED_FILE_TYPES             | .jpg,.jpeg,.png               |
| MAX_FILE_SIZE_MB               | 10                            |
| THUMBNAIL_SIZE                 | 720                           |
| SECRET_KEY                     | (optional)                    |
| SECRET_KEY_{GalleryId}         | (optional)                    |
| ADMIN_PASSWORD                 | admin                         |
| SINGLE_GALLERY_MODE            | false                         |
| REQUIRE_REVIEW                 | true                          |
| DISABLE_HOME_LINK              | false                         |
| DISABLE_REVIEW_COUNTER         | false                         |
| DISABLE_UPLOAD                 | false                         |
| DISABLE_QR_CODE                | false                         |
| DISABLE_GUEST_GALLERY_CREATION | true                          |
| HIDE_KEY_FROM_QR_CODE          | false                         |
| IDLE_GALLERY_REFRESH_MINS      | 5 (0 = disable)               |
| DIRECTORY_SCANNER_INTERVAL     | */30 * * * * (cron)           |

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
      ALLOWED_FILE_TYPES: '.jpg,.jpeg,.png'
      MAX_FILE_SIZE_MB: 10
      SECRET_KEY: 'password'
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
- GitHub - https://github.com/Cirx08/WeddingShare
- DockerHub - https://hub.docker.com/r/cirx08/wedding_share
- BuyMeACoffee - https://buymeacoffee.com/cirx08

## Screenshots

### Desktop

![Homepage](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Homepage.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery.png?raw=true)

![Admin Area](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Admin.png?raw=true)

### Mobile

![Homepage](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Homepage-Mobile.png?raw=true)

![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Mobile.png?raw=true)

![Admin Area](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Admin-Mobile.png?raw=true)

### Attributions
- [Bride icons created by Freepik - Flaticon](https://www.flaticon.com/free-icon/wedding-couple_703213)