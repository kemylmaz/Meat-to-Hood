# Meshy mobile asset pipeline

`process_meshy_assets.py` fixes the eight mislabeled downloads, regenerates
normals, places every pivot at bottom-center, and exports three mobile LODs per
asset. The source GLBs remain outside the Unity project as recoverable masters.

The generated FBXs intentionally contain geometry only. Unity creates the cozy
URP palette materials and LOD prefabs so gameplay objects can keep their simple
functional colliders while Meshy meshes remain visual-only children.
