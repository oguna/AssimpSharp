# AssimpSharp

This is the library to parse some 3d formats written in C#.

## Feature

This is managed code library.

This can parse the following formats:

- FBX
- OBJ
- X

## Assimp
This code is based on Assimp v3.2 (c318d64).

I manually built assim-net for the latest assimp.
AssimpNet passed all tests with Assimp v3.2.

## Problem

This library has some problems.

- Some tests are not passed because of bugs.
- FBX: This library contains reflection (dynamic).
- Performance. More than twice as time cost as native.

## How to run viewer

Build solution, and get AssimpView.exe in build output directory.

Run AssimpView.exe with commandline argument `../../../test/models_nonbsd/FBX/2013_ASCII/duckFixed.fbx`,
then you can see duck.

## License

AssimpSharp is released as Open Source under the terms of a 3-clause BSD license.

Caution: Files in `Test/models_nonbsd` are not 3-clause BSD.


