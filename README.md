# 2D Infinite Procedural Generation

This is a framework for generating 2d procedural infinite worlds.
It was developed with 2D Tilemaps in mind but can be adapted to work with custom implementations.
I quickly cobbled up a similar procedural generation while working
on [Dust Devil](https://simeonradivoev.itch.io/dust-devil) for the Brackeys Game Jam 2024.2.
I found it pretty useful so I decided to polish it up a bit, add blending and optimize it using burst.

## Sample

I made a simple Shoot 'em up game that has infinite vertical terrain generation. You can check it out
here [source](https://github.com/simeonradivoev/2d-procgen-sample) or just import it in unity from the package manager
in the samples section.

## Features

- Infinite Generation
- Biomes
- Layer Based Noises
- Shared data between layers
- Biome blending

## Instalation

Just copy the Git URL `https://github.com/simeonradivoev/2d-procgen.git` into the package manager in unity by
pressing `Add package from git URL`.
To import the sample game just head over to the samples section in the package manager window.

## Usage

I might add detailed documentation at some point, but I kept the API pretty bare bones as it is useful for game jams and
to adapt to your own uses.
Check out the sample project for a more advanced example of and implementation.

1. Add a `TilemapContext` component to an object
2. Add a `VerticalBiomeSource` the the same object
3. Call the `GenerateAsync` and `DisposeAsync` appropriately from your own manager

### Chunk Prefabs

You must have a chunk prefab that has the `TilemapChunk` component on the root and each Tilemap child must have
the `TilemapChunkTilemap` component.
Names of the tilemaps matter as they will be used in the biomes as well.

### Biome Prefabs

To generate biomes you have to make `GeneratorBiome` root object that has multiple children `GeneratorGroup` named the
same as your tilemap in the chunk prefab.
Each group with the name of the tilemap will be used to generate for that tilemap in the chunk.
Each group must have layers as children having the `TilemapGeneratorLayer` component on them along with as many sources
as you like

![](/Samples~/TreeStructure.png)
This is an example of the hierarchy structure. Check out
the [Sample](https://github.com/simeonradivoev/2d-procgen-sample) project for examples.

#### Shared Data

`TilemapGeneratorLayer` can have it's values exposed to other layers in the group.
Say you generate lakes and you want to use them as a mask so that tees are not in the lakes. You can use
a `GeneratorSourceSharedData` to access that data.