<p align="center"><img width="80%" src="https://github.com/user-attachments/assets/6fadccdb-3a27-4170-afbc-98725970b0e5"></p>

<h3 align="center">Modern C# game engine with a Unity like api and structure.</h3>

<div align="center">

![lines](https://sloc.xyz/github/sjoerdev/concrete/?lower=true)
![stars](https://img.shields.io/github/stars/sjoerdev/concrete?style=flat)
![version](https://img.shields.io/github/v/release/sjoerdev/concrete?include_prereleases)
![license](https://img.shields.io/badge/license-MIT-blue.svg)

</div>

## Features

- unity inspired structure
- component based architecture
- powerful imgui based editor
- lightweight opengl renderer
- skinned mesh rendering
- complete gltf support

## Scripting
```csharp
var scene = new Scene();
LoadScene(scene);

var cameraObject = scene.AddGameObject();
cameraObject.AddComponent<Camera>();
cameraObject.name = "Main Camera";

var lightObject = scene.AddGameObject();
lightObject.AddComponent<DirectionalLight>();
lightObject.transform.localEulerAngles = new Vector3(20, 135, 0);
lightObject.name = "Directional Light";
```

## Editor

<img width="1600" height="900" alt="editor" src="https://github.com/user-attachments/assets/3ba95a9a-f89a-439c-b82a-4b5c0f80b174" />

