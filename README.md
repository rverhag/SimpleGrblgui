# SimpleGrblgui
A basic gui for Grbl v1.1f. Libraries are written in C# for .Net 4.5. and gui is based on WPF. 

This desktop app is only to be used by people who wants a very basic gui for controlling GRBL from a Windows OS. 

![](https://github.com/rverhag/SimpleGrblgui/blob/master/WikiImages/Butterfly.jpg)

No fancy screens, menus, buttons or whatsoever. Just plug in and go..... almost

To get started you'll need grbl v1.1f(2017-08-01) flashed on an Arduino and Visual studio 2017 to get things compiled.
Maybe you'll need to tweak the configfile a bit and change the comport to the right one.

Most of the code is my own, but some of it came from examples and other projects i found. In case of copied code, there should be a reference to the source.

Thanks to all the people who has provided insights, sourcecode, tips and tricks during development.

Thanks for the WpfCap-repository
https://github.com/chris-evans/WpfCap
for making it possible for showing a videostream in WPF

And thanks to everybody involved in the Helixtoolkit project
https://github.com/helix-toolkit
for making it possible to create a 3D representation from the gcode.

Special thanks to Sonny Jeon (chamnit) for all his Grbl-work https://github.com/gnea/grbl. Without his work, this gui had no meaning at all.
