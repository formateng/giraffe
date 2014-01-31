# Planar Mesh Tool for Grasshopper

**Author**: Harri Lewis

**Working?**: Yes (Rhino 5.0, Grasshopper 0.9.0061)

## Description

Contains a Grasshopper component which implements a remeshing algorithm that approximates a mesh with planar panels.  Each panel represents a cluster of faces in the parent mesh.  Clusters can be arranged by distance or by surface normal.  Note that manual post-processing is sometimes required for complex forms.

See [Cutler & Whiting - Constrained Planar Remeshing for Architecture][srcpap] ([abstract][srcabs])

Includes an winged edge mesh implementation.

## Related Projects

* [Trada Pavilion][trada1] ([2][trada2])
* [Fitrovia Chalkboard Installation][fitz1]



[trada1]: http://www.ramboll.co.uk/projects/viewproject?projectid=818A55DE-F462-4AC0-A99A-C121C8563186
[trada2]: http://www.grasshopper3d.com/profiles/blogs/trada-pavilion
[fitz1]: http://www.ramboll.co.uk/news/viewnews?newsid=25241383-dd23-4310-a79d-388cde1af49c
[srcpap]: http://www.cs.rpi.edu/~cutler/publications/planar_remeshing_gi07.pdf
[srcabs]: http://www.cs.rpi.edu/~cutler/publications/sgp06-poster-abstract.pdf
