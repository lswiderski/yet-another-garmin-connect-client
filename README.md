# yet-another-garmin-connect-client
YAGCC: Yet Another Garmin Connect Client

# Docker-Compose Example
version: "3.9"

services:
 yagcc-api:
    container_name: YAGCC
    restart: unless-stopped
    image: lswiderski/yet-another-garmin-connect-client-api
    ports:
      - 80:80
