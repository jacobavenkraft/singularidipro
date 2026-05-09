# singularidipro
New version of singularidi using Unity game engine.

## Singularidi
Singularidi is intended to be a MIDI renderer application that produces visualizations of MIDI files.  The general idea behind the visualizations is to have some type of falling note representation that hits a bank of piano keys and plays the notes when the visualization comes into contact with the corresponding piano key.  The inspiration behind this renderer was my son, who wanted a renderer that could reliably reproduce "black midi" files.

## Functionality (in progress)
The following visualizations are currently in development
1. Top-down view: A top down view where a piano keyboard is displayed at the bottom of the window and multi-colored bars descend from the top (each bar represents a single note on the 128 note midi scale).  When a colored bar intersects with the corresponding key, the midi note for that key will play.
2. Perspective view: Imagine tilting the camera of the top-down view so that it is in front of the piano keys and shows the multi-colored bars coming toward the screen from the edge of the horizon.  This is the stereotypical view from Guitar Hero where the note representations appear to be coming toward the observer.
3. Cylinder view: Now take the perspective view and wrap the keyboard keys around the inside of a cylinder and have the multi-colored bars for the notes coming from the center of the cylinder toward the keys.
4. 
