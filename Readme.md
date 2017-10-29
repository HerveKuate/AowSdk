## Intro

This is a public snapshot of the AoW 2 SDK created as a part of MP Evolution project to analyze and manipulate the game files.
Originally there were no plans to release it, because it could easily be used for subtle and untraceable cheating, which would ruin the competitive gaming.
But now that the online community has been dead for quite a while, the reason not to release it is gone.
And as I recently learned that there's still a few people who are interested in modifying the game,
I've decided to finally open source this SDK to facilitate the work of these dedicated people.

Please keep in mind that this is a very old codebase, and some parts of it are not for the faint of heart ;)
But it does contain some very valuable knowledge about the game resources, so I hope you'll find it useful.

Here's what you'll find inside:

## Aow.Serialization

This is the project that implements the (de)serializer for the AoW 2 resource format that the game uses for the mod files, maps, image libraries, etc.
An entry point for examining it is `AowSerializer<T>` - if you start there, eventually you'll find calls to all the details of the file format.
Here's a few tips to help you in this journey.

### Implementation notes

For performance reasons, `AowSerializer<T>` generates and caches the serialization routines for each type it encounters.
The set of per-type (de)serialization methods is referred to as `IFormatter`; the formatter cache is maintained by `FormatterManager`.

Formatters are generated by `IFormatterBuilder`s.
Every `IFormatterBuilder` handles a subset of the supported types: for example, `PrimitiveTypeFormatterBuilder` creates builders for primitive types, `EnumFormatterBuilder` created builders for enums, etc.
The full list of available builders (as well as the logic of dispatching formatter requests to them) is contained in `BuilderResolver`.

Most of the builders are trivial, the main exception is `OffsetMapFormatterBuilder` - this is the builder responsible for seralization of complex data types.
In order to serialize a value of complex type, the builder needs to deconstruct it into its fields and then serialize each of them recursively.
The type deconstruction is (like everything else in the serializer) modular, and is handled by `IFieldProvider`s.
There are three of them: one for lists, one for dictionaries and one for classes.
It's important to note that a single type may by deconstructed by multiple `IFieldProvider`s,
because the game engine has types that store both static set of fields and dynamic range of items, thus acting like normal classes and collections simultaneously.

All generated code is added to the dynamic assembly `Aow.Serialization.Generated.dll`.
It's normally kept in memory, but for debugging purposes it's often useful to call `GeneratedAssembly.Save()`
to save this assembly to the working directory and then examine it with a decompiler.

### File format notes

Resource file format basically has two kinds of types: complex and primitive.

Primitive types have custom serialization routines that store values as simple blobs.
As you might have guessed, the typical examples of that are primitives like `int` or `bool` - they are simply stored as their binary representation.

Complex types are stored as key-value pairs of their fields with integer keys (referred to as field IDs in the codebase).
The binary representation of a complex type consists of two parts: the offset map and the data.
The offset map stores the number of fields in the instance, followed by a corresponding number of `{ FieldID, FieldOffset }` pairs.
The data consists of concatenated binary representations of the instance fields in the same order they are mentioned in the offset map.

Even though in reality there are three kinds of complex types: classes, lists and dictionaries -
there's no distinction between them from the file format point of view, the only difference is where they get the field IDs from.
Classes have their fields serialized with every field having a hardcoded ID.
Lists have their items serialized with item indices being used as IDs.
And dictionaries obviously use the keys as IDs and the values as, well, values.

The last important thing here is the fact that collections (both lists and dictionaries) can store instances of virtual classes.
Since different items in such collections may have different sets of fields, the serialized items have their binary representations prefixed by class IDs - hardcoded `int` values that unambiguously resolve the specific class.

## Aow.Core

This is the core project of the SDK (hence the name).
It mostly consists of tons of classes that describe resource entities of the game and have their structure and serialization IDs reverse engineered.
It also implements the additional logic that the game applies to its files on top of its serialization format (checksums, compression, encryption),
providing higher-level API for working with the game resources.

Most of the work here revolved around the mod files.
An entry point to examining mods is static `ModManager` that allows enumerating through the meta info about all installed mods and the active mod.
Both APIs return instances of `ModInfo`, which has an `Open` method that loads the contents of the mod from disk and returns an `AowMod` instance
that provides access to the actual mod data.

Another big cluster of data here was produced by researching the map format.
The API here is easier: there's a static `AowMap.Open` method that loads a map (or a save, they are basically the same thing)
and returns it as an instance of `AowMap` class.

You should find the classes in this assembly to have all their fields listed with correct field IDs.
However, not all of them have been fully reverse engineered, so you'll find two kinds of missing info here.

The first kind is the field semantics: some of the fields don't have meaningful names because their meaning have not been discovered,
typically because they proved to be difficult to analyze and/or not important for our work.

The second kind is the field types: some fields have a type of `UnknownData`.
This class is a universal placeholder that deserializes value as a byte array and serializes it back untouched,
thus allowing to edit parts of the resources that were reverse engineered without losing data of the parts that were not.
Most of `UnknownData` fields are actually normal collections and classes that simply didn't contain anything useful for MPE, so they were ignored.
There are exceptions though: for example, `MapLevel.Data` is actually a collection of map level hexagons and objects
that is stored in a different way from the normal resource collections and therefore needs an `ICustomFormatted` implementation that I never got around to write.
