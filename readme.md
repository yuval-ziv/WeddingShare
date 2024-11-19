# WeddingShare

## About

WeddingShare is a very basic site with only one goal. It provides you and your guests a way to share memories of and leading up to the big day. Simply provide your guests with a link to a gallery either via a Url or even better by printing out the provided QR code and dropping it on your guests dinner tables. Doing so will allow them to view your journey up to this point such as dress/suit shopping, viewing the venue, doing the food tasting or cake shopping, etc. 
You are not limited to a single gallery. You can generate multiple gallerys all with their own sharable links. At this stage galleries are a bit unsecure, meaning anyone with the link has acess to view and share images so I recommend keeping your share links private. To combat strangers gaining access to your galleries you can provide a secret key during setup but be advised this is a deterant to make guessing Urls slightly harder and not an actual security catch all. 

## Disclaimer

Warning. This is open source software (GPL-V3), and while we make a best effort to ensure releases are stable and bug-free,
there are no warranties. Use at your own risk.

## Settings
| Name                  | Value                         |
| -------------------   | ----------------------------- |
| TITLE                 | WeddingShare                  |
| LOGO                  | https://someurl/someimage.png |
| GALLERY_COLUMNS       | 4                             |
| ALLOWED_FILE_TYPES    | .jpg,.jpeg,.png               |
| MAX_FILE_SIZE_MB      | 10                            |
| SECRET_KEY            | (optional)                    |
| DISABLE_UPLOAD        | false                         |
| DISABLE_QR_CODE       | false                         |
| HIDE_KEY_FROM_QR_CODE | false                         |
| DISABLE_HOME_LINK     | false                         |

## Docker Run

```
docker run --name WeddingShare -h wedding-share -p 8080:5000 -v /var/lib/docker/volumes/wedding-share/_data:/app/wwwroot/uploads:rw --restart always cirx08/wedding_share:latest
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
      - data-volume:/app/wwwroot/uploads
    network_mode: bridge
    hostname: wedding-share
    restart: always

volumes:
  data-volume:
    name: WeddingShare
```

## Links
- GitHub - https://github.com/Cirx08/WeddingShare
- DockerHub - https://hub.docker.com/r/cirx08/wedding_share
- BuyMeACoffee - https://buymeacoffee.com/cirx08

## Screenshots

### Desktop

![Homepage](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Homepage.png?raw=true)
![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery.png?raw=true)

### Mobile

![Homepage](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Homepage-Mobile.png?raw=true)
![Gallery](https://github.com/Cirx08/WeddingShare/blob/main/screenshots/Gallery-Mobile.png?raw=true)