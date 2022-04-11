# QMK Sync over HID

Communicates with a QMK keyboard over Raw HID.

## Setup

Requires `RAW_ENABLE = yes` in your `rules.mk`.

This is currently hardcoded for the vendor-id and product-id of my keyboard, but that can be changed easily.

## Features

### 0x01: Synchronize clock

Sends the elapsed millseconds since midnight, which is used in conjunction with `timer_read32()` to calculate the time of day on the keyboard.
