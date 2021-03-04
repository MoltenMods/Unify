# Unify

An Among Us mod to add extra regions to the regions menu.

![Regions menu](images/RegionsMenu.jpg)

# Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)

## Features

The following regions have been added:
- [skeld.net](https://skeld.net/)
- localhost *(for playing on a server running on your computer)*

## Installation

> **⚠ WARNING**
>
> This mod requires you to have BepInEx and Reactor installed. You can install BepInEx and Reactor by following [this guide](INSTALLATION.md) or by following the steps on the
> following two pages: https://docs.reactor.gg/docs/basic/install_bepinex and https://docs.reactor.gg/docs/basic/install_reactor.

1. Ensure that BepInEx and Reactor are installed (more information above).
2. Download the latest mod on the [releases](https://github.com/DaemonBeast/Unify/releases) page.
3. Move the mod to the `(Among Us game files)/BepInEx/plugins` folder.
4. Launch the game.

## Configuration

> **⚠ WARNING**
>
> The configuration file will be generated when loading the mod for
> the first time. If the configuration file is not there, try
> launching the game and closing it to see if the configuration
> file creates itself.

The configuration file is located at
`(Among Us game files)/BepInEx/config/daemon.unify.reactor.cfg`.

### Custom regions

Custom regions can be added by modifying the `[Region 1]`,
`[Region 2]` ... `[Region 5]` sections of the configuration file.
The sections allow you to modify the display name of the region,
the IP address of the region, and the port of the region.

If the IP field is left empty, the region will not show up in game.
