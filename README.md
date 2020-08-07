# EasyBuildManager
EasyBuildManager (EBM) is a Visual Studio extension allowing you to quickly select which project to build within the solution.

## Description
Unlike other extensions which try to enhance the default VS Build Manager, EBM is focus on simplicity to select which projects to build in one click for the current Platform|Configuration via a ToolWindow.

I've designed this extension to help on my daily job where I manage a solution with 100+ projects that I want to keep loaded - for "Find all references" and also because it's not free to load/unload projects (Intellisense reparsing files...) - but I want to compile only the ones I'm working on. And I may switch between projects several times a day.

![Screenshot of EasyBuildManager](/EasyBuildManager/Resources/Screenshot.png "Screenshot of EasyBuildManager")

### Key features
- Auto select dependency projects based on **References**: when a project is selected for build, all of its dependency tree projects are also selected.
- Auto unselected projects depending on: when a project is unselected, it also unselect projects depending on this project.
- Clean non-built projects (useful to avoid outdated binaries).
- Build a Direct Graph (.dgml) of the project dependencies.
- Try to rebuild *References* between projects based on the library (C++) they import (Experimental).

### Usage
This extension modify the solution (.sln) like the built-in Build Manager does. So it's preferable to work on a copy of the solution you are sharing with your collobators to keep these changes local.

### Release notes:
#### v0.3
- Added a checkbox to select/unselect all
- Fix refresh when the current Configuration|Platform changes

### TODO
- Add options to control whether dependencies should be automatically selected / unselected
- Add preset manager with config file which can be shared across a repo.
- Add unit tests
