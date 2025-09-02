# YAGCC
YAGCC: Yet Another Garmin Connect Client

YAGCC is a:
- Library
- CLI client
- Selfhosted WEB API
  
using which you can create a .fit file with activity and upload it to Garmin Connect.

Currently it is at a very early stage. It only has the features that I needed for my other projects.

## Examples
YAGCC was created for and in my 2 other projects:
- Mi Scale Exporter https://github.com/lswiderski/mi-scale-exporter
- Web Body Composition https://github.com/lswiderski/WebBodyComposition


## Usage

![CLI](https://github.com/lswiderski/yet-another-garmin-connect-client/blob/main/resources/img/yagcc.png?raw=true)

![uploadbodycomposition](https://github.com/lswiderski/yet-another-garmin-connect-client/blob/main/resources/img/uploadbodycomposition.png?raw=true)

![uploadbloodpressure](https://github.com/lswiderski/yet-another-garmin-connect-client/blob/main/resources/img/uploadbloodpressure.png?raw=true)

![API](https://github.com/lswiderski/yet-another-garmin-connect-client/blob/main/resources/img/api.png?raw=true)

## Docker-Compose Example
```yaml
services:
  yagcc-api:
    image: ghcr.io/chriszuercher/yet-another-garmin-connect-client-api:latest
    container_name: yagcc
    restart: unless-stopped
    environment:
      - AppSettings__Auth__Email=your@example.com
      - AppSettings__Auth__Password=verySecretPassword
      - AppSettings__UserProfileSettings__Age=yourAge
      - AppSettings__UserProfileSettings__Height=yourHeightInCm
      - AppSettings__UserProfileSettings__Gender=1
      - AppSettings__General__DisableSwagger=false
    ports:
      - "8081:8080"
```

## Configuration
YAGCC can be configured via appsettings.json or environment variables.

Supported environment variables:


| Environment Variable                          | Example/Values                        | Description                                 |
|-----------------------------------------------|---------------------------------------|---------------------------------------------|
| AppSettings__Auth__Email                      | your@example.com                      | Garmin account email                        |
| AppSettings__Auth__Password                   | verySecretPassword                    | Garmin account password                     |
| AppSettings__Auth__AccessToken                | (token value)                         | Optional: Garmin OAuth access token         |
| AppSettings__Auth__TokenSecret                | (token secret)                        | Optional: Garmin OAuth token secret         |
| AppSettings__UserProfileSettings__Age         | yourAge                               | User age                                    |
| AppSettings__UserProfileSettings__Height      | yourHeightInCm                        | User height in centimeters                  |
| AppSettings__UserProfileSettings__Gender      | 0 for Female, 1 for Male              | User gender                                 |
| AppSettings__General__DisableSwagger          | true / false (default: false)         | Disable Swagger UI under `/swagger`         |