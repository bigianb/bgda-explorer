﻿namespace JetBlackEngineLib.Data.Models;

public class Model
{
    // collection of meshes
    public readonly IReadOnlyCollection<Mesh> MeshList;

    public Model(IReadOnlyCollection<Mesh> meshList)
    {
        MeshList = meshList;
    }

    /// <summary>
    /// Counts the number of bones in the model based of bone id references.
    /// </summary>
    public int CountBones()
    {
        // Find the largest bone id
        var largestID = 0;

        foreach (var mesh in MeshList)
        foreach (var weight in mesh.VertexWeights)
        {
            if (weight.bone1 != 255 && largestID < weight.bone1)
            {
                largestID = weight.bone1;
            }

            if (weight.bone2 != 255 && largestID < weight.bone2)
            {
                largestID = weight.bone2;
            }

            if (weight.bone3 != 255 && largestID < weight.bone3)
            {
                largestID = weight.bone3;
            }

            if (weight.bone4 != 255 && largestID < weight.bone4)
            {
                largestID = weight.bone4;
            }
        }

        if (largestID == 0)
        {
            return 0;
        }

        return largestID + 1;
    }
}