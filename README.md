# Planar Mesh Tool for Grasshopper

Copyright © 2012-2014 Ramboll UK Ltd.

Project started by [Harri Lewis](https://github.com/harrilewis).

## Description

Contains a Grasshopper component which implements a remeshing algorithm that approximates a mesh with planar panels.  Each panel represents a cluster of faces in the parent mesh.  Clusters can be arranged by distance or by surface normal.  Note that manual post-processing is sometimes required for complex forms.

This projcet is an implementation of processes described in [Cutler & Whiting - Constrained Planar Remeshing for Architecture][srcpap] ([abstract][srcabs]).

Includes an winged edge mesh implementation (although the plan is to replace this with [Plankton](https://github.com/Dan-Piker/Plankton)).

## Related Projects

* [Trada][trada1] [Pavilion][trada2]
* [Fitrovia Chalkboard Installation][fitz1]



[trada1]: http://www.ramboll.co.uk/projects/viewproject?projectid=818A55DE-F462-4AC0-A99A-C121C8563186
[trada2]: http://www.grasshopper3d.com/profiles/blogs/trada-pavilion
[fitz1]: http://www.ramboll.co.uk/news/viewnews?newsid=25241383-dd23-4310-a79d-388cde1af49c
[srcpap]: http://www.cs.rpi.edu/~cutler/publications/planar_remeshing_gi07.pdf
[srcabs]: http://www.cs.rpi.edu/~cutler/publications/sgp06-poster-abstract.pdf
