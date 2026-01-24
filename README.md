# Terrain Generation in Godot

This project implements procedural terrain generation following Sebastian 
Lague's youtube [playlist](https://www.youtube.com/watch?v=wbpMiKiSKm8&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3) 
but in Godot instead of Unity. 

The purpose of this project was to learn about perlin noise, terrain generation
and to get some more experience using Godot. Each of Sebastian's episodes 
is a different commit in the history (`git log`)

## Screenshots

### Perlin Noise
![alt text](.docs/demo_02_mesh_generation.png)

### Mesh Generation
![alt text](.docs/demo_01.png)

### Procedular Generation
<video controls src=".docs/demo_03_Proc Gen 02.webm" title="alt text"></video>

### Colors
![alt text](.docs/demo_04.png)

### LOD
![alt text](.docs/demo_06.png)

### Endless Terrain
<video controls src=".docs/demo_08.webm" title="Title"></video>

### LOD switching
<video controls src=".docs/demo_07.webm" title="Title"></video>
![alt text](.docs/demo_09.png)

### Falloff
![alt text](.docs/demo_11.png)

### Normals
![alt text](.docs/demo_12_normals.png)

### Collisions
<video controls src=".docs/demo_13_collissions.webm" title="Title"></video>

### Flat Shading
Normal Shading | Flat Shaded
---------------|-------------
![alt text](.docs/demo_14_flat_shading_1.png) |  ![alt text](.docs/demo_14_flat_shading_2.png)

### Data Storage
![alt text](.docs/demo_15_data_storage.png)

### Color Shader
![alt text](.docs/demo_16_color_shading.png)

### Texture Shader
![alt text](.docs/demo_17_texture_shader.png)



## Caveats
Due to the differences between Unity and Godot there will be many difference in 
how things get implemented but it tries to follow the same general architectural
patterns. One of the big differences is the difference in support for "In-Editor"
controls in Godot compared to Unity. You'll see alot of `[Export]` properties 
with their corresponding `get;set;` in order to emulate the behavior in Unity.