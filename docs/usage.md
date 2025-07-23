# Usage
Install the program with `cargo install --path .`, and then call the program with `svled`.

The following commands are provided:
```
Usage: svled [OPTIONS]

Optional arguments:
  -h, --help           print help message
  -v, --verbose        be verbose
  -c, --config CONFIG  specify a specific config file

Available commands:
  speedtest       perform a connection speedtest
  read-vled       play back a vled file
  calibrate       calibrate a svled container
  unity           send positions and connect to Unity
  send-pos        send positions to Unity
  connect-unity   connect to Unity
  driver-wizard   interactively create a ino/cpp file for your LED driver
  set-color       set a single leds color
  clear           clear the strip
  demo            run a simple demo
  convert-ledpos  convert an led position json into a C++ compatible constant
  list-cams       list functioning camera indexes
  post-process    re-run post processing on an position file
```