# Javascript on Unity

Provides to bind Javascripts to Unity.
- Supports simple binding
- Supports Promise on Javascript side

I referred to [Using real JavaScript with Unity](https://pleasenophp.github.io/posts/using-real-javascript-with-unity.html)

## Requirements

- Json.NET for Unity (https://github.com/jilleJr/Newtonsoft.Json-for-Unity)
- UniTask (https://github.com/Cysharp/UniTask)
- Environments to be able to run shell scripts (*.sh)

## Getting started

- Install via [UPM package](#upm-package)
- Import the sample ```Testbed```
- Open the Javascript Builder window

  ![image](https://user-images.githubusercontent.com/22534449/131437303-38c1d00b-ac3d-4294-b299-ef76daa90c1f.png)
- Set Build Options then ```Install npm modules```. This work takes a lot of time.

  ![image](https://user-images.githubusercontent.com/22534449/131490619-e907a0c3-b4a9-4a05-bc96-7b963e0aea56.png)
  ![image](https://user-images.githubusercontent.com/22534449/131490534-fb87fd8a-3372-4228-b453-2aa293f768d4.png)
  
  (Before and After)
  - Raw Scripts Root: The root directory of javascript codes you made or will make directly. Recommended to be under Editor path because of npm modules will be installed in this path. The sample has been tested as that set ```Testbed/Editor/Javascripts/RawScripts```.
  - Built Scripts Root: The root directory of javascript codes that will be generated via webpack. The sample has been tested as that set ```Testbed/Runtime/Javascripts/Outputs```.
  - Dev Build: The option of webpack, if it is true, built scripts will NOT be minimized.
- Set Generate Helpers Options
  - Root to generate: The root path of generated helper javascripts. Helpers are like of header files of C++, but it doesn't matter if you don't have them. The sample has been tested as that set ```Testbed/Editor/Javascripts/RawScripts```.
  - Engine codes: The files of classes derived ```JavascriptEngine``` class. As ```MonoBehaviour``` does, each files must have one class with the same name. The sample has been tested as that set ```SampleEngine```, but you can set multiple files also.
- Play the SampleScene
  - A box would be created and moves right and left. The sample contains a class derived ```JavascriptEngine``` and a class derived ```BoundClass```, and contains ```sample.js``` also.
- To bind a new Javascript
  - Save new one under the ```Raw Scripts Root``` path and ```Build``` via ```Javascript Builder``` window. To bind any new Engine or Class, please to see the sample.

## Documents

Sorry that it is not ready yet...but I want to do ASAP.

## UPM Package

Only the git URL is supported yet.

- main: ```https://github.com/thevoidky/javascript-on-unity.git```
- experimental: ```https://github.com/thevoidky/javascript-on-unity.git#upm-experimental```
- dev(NOT RECOMMENDED): ```https://github.com/thevoidky/javascript-on-unity.git#upm-dev```

![image](https://user-images.githubusercontent.com/22534449/131436588-ba56deda-1c84-4b22-a00d-53cd5b87bfc4.png)
![image](https://user-images.githubusercontent.com/22534449/131436646-891df365-47d4-4a59-8b03-8ab43f6d1005.png)
