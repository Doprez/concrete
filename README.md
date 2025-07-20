<p align="center"><img width="60%" src="https://github.com/user-attachments/assets/6fadccdb-3a27-4170-afbc-98725970b0e5"></p>

<h1 align="center">C# Game Engine</h1>

<p align="center">Modern C# game engine with a Unity like api and structure.</p>

## Features

- unity inspired structure
- component based architecture
- simple imgui based editor
- simple opengl renderer
- complete gltf support
- skinned mesh rendering
- skeletal animation

## Example
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

## Gallery
![editor](https://github.com/user-attachments/assets/7a93194b-eb90-435d-9465-3a906dc275ff)
