# homeassistant-unifi-led
Control UniFi access point LEDs via Home Assistant. This takes data via MQTT (with MQTT discovery) and controls lights of the access points via SSH. See config.example.json for configuration sample.

On startup, this should create a new Home Assistant device for each Access Point.
Each device has a state (ON/OFF) and an effect (white/blue).

![Home Assistant Screenshot](screenshot.png "Home Assistant Screenshot")

# Warning:
This is just a proof of concept.

It is a bad idea to have your UniFi root SSH credentials in a config file on some random server, also there is very little error handling and logging if something goes wrong.

Thus, you should probably not use this in a productive environment.

# Installation:
Make sure you cloned the repository **including all submodules**!

Take a look at the `docker-compose.yml` for how to run this thing.

There's an example config in `src/HomeAssistantUnifiLed/config.example.json`