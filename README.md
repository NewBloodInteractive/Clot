# Clot - The Content Exporter
Why is it called Clot? Because I lack creativity and it was made for a "New Blood" project. Get it?

## Rationale
During the development of the Dusk SDK, it was necessary to export the game's assets so that they could be loaded externally, to decouple them from the Unity project.

This posed a problem, as many of the assets had been modified within Unity, particularly models and animations, meaning that these changes would not be reflected in their source (.blend) files.

This library was created in order to provide a simple API for exporting these assets en masse into the Inter-Quake Model (.iqm) format.

At a later date, the code used to export level geometry and entities may also be published.

## Usage Example
Note that this API was designed to be used once to export all of the game's models and animations, and therefore may not be as convenient to use as it otherwise could have been.

The below example demonstrates how to export most models correctly, however it does not account for MeshFilter/MeshRenderer combos (non-animated models), preserve offsets and rotations from the transform hierarchy, or allow you to create attachment bones.

A more advanced example offering the aforementioned features may be published in the future.

```cs
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using NewBlood.Clot;

class ExportSample
{
    [MenuItem("Tools/Clot/Export Selected Skinned Mesh")]
    static void ExportSkinnedMesh()
    {
        var iqm       = new IqmBuilder();
        var prefab    = Selection.activeGameObject;
        var instance  = UnityEngine.Object.Instantiate(prefab);
        var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
        var animator  = instance.GetComponent<Animator>();

        // Phase 1: Fill in the bone and geometry data
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            // We potentially make changes to the mesh by calling RecalculateNormals
            // and RecalculateTangents, so we instantiate it instead of using it directly.
            Mesh sharedMesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
            int vertexCount = sharedMesh.vertexCount;
            
            // Bones and vertices need to be offset
            int vertexOffset = iqm.Vertices.Count;
            int boneOffset   = iqm.Bones.Count;
            
            // Ensure we have normals and tangents available
            if (sharedMesh.normals.Length != vertexCount)
                sharedMesh.RecalculateNormals();

            if (sharedMesh.tangents.Length != vertexCount)
                sharedMesh.RecalculateTangents();
            
            for (int i = 0; i < vertexCount; i++)
            {
                var vertex = new Vertex
                {
                    Position = sharedMesh.vertices[i],
                    Normal   = sharedMesh.normals[i],
                    Tangent  = sharedMesh.tangents[i]
                };

                if (sharedMesh.uv.Length == vertexCount)
                    vertex.TexCoord = sharedMesh.uv[i];

                if (sharedMesh.colors32.Length == vertexCount)
                    vertex.Color = sharedMesh.colors32[i];

                if (sharedMesh.boneWeights.Length == vertexCount)
                {
                    var weight         = sharedMesh.boneWeights[i];
                    weight.boneIndex0 += boneOffset;
                    weight.boneIndex1 += boneOffset;
                    weight.boneIndex2 += boneOffset;
                    weight.boneIndex3 += boneOffset;
                    vertex.Weight      = weight;
                }

                iqm.Vertices.Add(vertex);
            }
            
            for (int i = 0; i < sharedMesh.subMeshCount; i++)
            {
                var triangles     = sharedMesh.GetTriangles(i);
                var triangleCount = triangles.Length / 3;
                var submesh       = new Submesh
                {
                    Name          = sharedMesh.name + " Submesh " + i,
                    TriangleIndex = iqm.Triangles.Count / 3,
                    TriangleCount = triangleCount
                };

                for (int t = 0; t < triangleCount; t++)
                {
                    iqm.Triangles.Add(new Triangle
                    {
                        VertexA = vertexOffset + triangles[3 * t + 0],
                        VertexB = vertexOffset + triangles[3 * t + 1],
                        VertexC = vertexOffset + triangles[3 * t + 2]
                    });
                }

                iqm.Submeshes.Add(submesh);
            }

            for (int i = 0; i < renderer.bones.Length; i++)
            {
                var matrix = sharedMesh.bindposes[i].inverse;
                var parent = Array.IndexOf(renderer.bones, renderer.bones[i].parent);

                // Mesh.bindposes is an array containing the inverse world transformation
                // matrices of each bone in its default pose. For non-root bones we need
                // to convert this into a local matrix.
                if (parent != -1)
                    matrix = sharedMesh.bindposes[parent] * matrix;

                iqm.Bones.Add(new Bone
                {
                    Name      = renderer.bones[i].name,
                    Transform = renderer.bones[i],
                    Matrix    = matrix
                });
            }

            // We now need to connect up the parent/child hierarchy for this mesh's bones
            for (int i = 0; i < renderer.bones.Length; i++)
            {
                int parent = Array.IndexOf(renderer.bones, renderer.bones[i].parent);

                if (parent == -1)
                    continue;

                iqm.Bones[boneOffset + i].Parent = iqm.Bones[boneOffset + parent];
            }

            UnityEngine.Object.DestroyImmediate(sharedMesh);
        }

        // Phase 2: Fill in the animation data
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            int frameOffset = iqm.FrameCount;
            int frameCount  = 1 + Mathf.CeilToInt(clip.frameRate * clip.length);
            iqm.FrameCount += frameCount;

            iqm.Animations.Add(new AnimationRange
            {
                Name       = clip.name,
                Flags      = clip.isLooping ? AnimationFlags.Loop : 0,
                FrameIndex = frameOffset,
                FrameCount = frameCount,
                FrameRate  = clip.frameRate
            });

            for (int i = 0; i < frameCount; i++)
            {
                float t = clip.length * (i / Mathf.Max(0f, frameCount - 1));
                clip.SampleAnimation(instance, t);

                foreach (var bone in iqm.Bones)
                {
                    // Is this bone associated with a valid Transform component?
                    if (bone.Transform)
                    {
                        iqm.SetKey(frameOffset + i, bone,
                            bone.Transform.localPosition,
                            bone.Transform.localRotation,
                            bone.Transform.localScale
                        );
                    }
                }
            }
        }

        UnityEngine.Object.DestroyImmediate(instance);

        // Phase 3: Write
        using (FileStream fs = File.OpenWrite(prefab.name + ".iqm"))
            iqm.Write(fs);
    }
}
```

## License
Clot is licensed under the MIT license.