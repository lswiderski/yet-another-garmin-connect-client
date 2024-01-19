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
version: "3.9"

services:
yagcc-api:
    container_name: YAGCC
    restart: unless-stopped
    image: lswiderski/yet-another-garmin-connect-client-api
    ports:
      - 80:80
```
