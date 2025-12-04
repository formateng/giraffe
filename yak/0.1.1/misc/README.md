# Planar Mesh Tool for Grasshopper

![yak](https://img.shields.io/badge/dynamic/json?label=yak&query=%24.version&url=https%3A%2F%2Fyak.rhino3d.com%2Fpackages%2FPlanarMesh&prefix=v)

![4_examples.png](https://raw.githubusercontent.com/formateng/giraffe/master/examples/4_examples.png)

Introducing two components to generate a planar polygon tessellation of a freeform mesh:

1. Lloyd’s - [Lloyd’s clustering algorithm](http://en.wikipedia.org/wiki/Lloyd's_algorithm): uses a Euclidean or normal based error metric to create a Voronoi diagram on any non-disjoint mesh.
2. Planarise Mesh - Planarise a set of closed non-planar curves. Can be used in combination with the Lloyd's algorithm component or independently (i.e. with weaverbird, see example below).

Code by Ramboll Computational Design originally developed for the TRADA Pavilion and based on the process described by Cutler and Whiting (2007). The Landesgartenschau Exhibition Hall by ICD in Stuttgart has shown the potential of planar remeshing of freeform shells in a similar manner.

This open source project is released under the [MIT licence](https://github.com/formateng/giraffe/blob/master/LICENSE).

There are four example files:

 * (a) Positively curved sphere (Euclidean based metric)
 * (b) Negatively curved saddle (normal based metric)
 * (c) Example using Weaverbird (showing planarise mesh component without Lloyds)
 * (d) Example using Starling (building a dual network from a planar mesh).

The components will appear under the `RCD > Remeshing` tab.

Copyright 2012-2014 Ramboll Computational Design (RCD).

Original code by Harri Lewis (we miss you) for the TRADA pavilion. Further developed by other RCD peoples. May contain bugs.

Enjoy.



#### Reference:

Cutler, B., & Whiting, E. 2007. _Constrained planar remeshing for architecture_. Pages 11–18 of: Proceedings of Graphics Interface 2007. ACM.
