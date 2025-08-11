using System.Numerics;
using SharpGLTF.Schema2;
using SharpGLTF.Runtime;
using SharpGLTF.Transforms;

namespace Concrete;

public class MeshRenderer : Component
{
    private Mesh[] meshes = [];
    private Shader shader = null;
    private bool skinned = false;
    private SceneInstance instance;

    [Show] [Include]
    public ModelGuid model
    {
        get => currentModel;
        set
        {
            currentModel = value;

            if (currentModel != null)
            {
                // calc model path
                string relativeModelPath = AssetDatabase.GetPath(currentModel.guid);
                string fullModelPath = Path.Combine(ProjectManager.projectRoot, relativeModelPath);

                // extract all meshes
                meshes = ModelReader.GetMeshes(fullModelPath);

                // create instance for animation
                instance = SceneTemplate.Create(ModelRoot.Load(fullModelPath).DefaultScene).CreateInstance();

                // check if mesh is skinned
                skinned = instance.GetDrawableInstance(0).Transform is SkinnedTransform;

                // create shader
                shader = skinned ? Shader.CreateSkinned() : Shader.CreateDefault();
            }
        }
    }

    private ModelGuid currentModel;

    public override void Render(float deltaTime, Matrix4x4 view, Matrix4x4 proj)
    {
        // dont render if no model is loaded
        if (currentModel == null) return;

        // set shader
        shader.Use();

        // set camera
        shader.SetMatrix4("view", view);
        shader.SetMatrix4("proj", proj);

        // set lights
        shader.SetLights(LightRegistry.registered);

        // set skin
        if (skinned)
        {
            var skinnedTransform = (SkinnedTransform)instance.GetDrawableInstance(0).Transform;
            Matrix4x4[] skinnedMatrices = skinnedTransform.SkinMatrices.ToArray();
            for (int i = 0; i < 100; i++) shader.SetMatrix4($"jointMatrices[{i}]", Matrix4x4.Identity);
            for (int i = 0; i < skinnedMatrices.Length; i++) shader.SetMatrix4($"jointMatrices[{i}]", skinnedMatrices[i]);
        }

        foreach (var mesh in meshes)
        {
            // set worldpos
            shader.SetMatrix4("model", mesh.offset * gameObject.transform.GetWorldModelMatrix());

            // set material
            shader.SetVector4("matColor", mesh.material.color);
            var hasAlbedo = mesh.material.albedoTexture != null;
            shader.SetBool("matHasAlbedoTexture", hasAlbedo);
            if (hasAlbedo) shader.SetTexture("matAlbedoTexture", (uint)mesh.material.albedoTexture, 0);
            var hasRoughness = mesh.material.roughnessTexture != null;
            shader.SetBool("matHasRoughnessTexture", hasRoughness);
            if (hasRoughness) shader.SetTexture("matRoughnessTexture", (uint)mesh.material.roughnessTexture, 1);

            // render mesh
            mesh.Render();
        }
    }
}