# homeassistant-unifi-led
control unifi access point LEDs via Home Assistant. This takes data via MQTT (with MQTT discovery) and controls lights of the access points via SSH. See config.example.json for configuration sample.

Each device has a state (ON/OFF) and an effect (white/blue).

![Home Assistant Screenshot](screenshot.png "Home Assistant Screenshot")

# Warning:
This is just a proof of concept, you probably shouldn't have your SSH credentials in some config file. Please do not use this in a productive environment.