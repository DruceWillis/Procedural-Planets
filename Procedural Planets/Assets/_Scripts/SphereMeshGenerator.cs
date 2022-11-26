using System.Collections.Generic;
using UnityEngine;

public abstract class SphereMeshGenerator
{
    protected readonly List<Vector3> _verticesPositions = new();
    protected readonly List<Vector3> _verticesNormals = new();
    protected int[] _triangles;
    
    public Vector3[] VerticesPositions => _verticesPositions.ToArray();
    public Vector3[] VerticesNormals => _verticesNormals.ToArray();
    public int[] Triangles => _triangles;
    
    protected List<Vertex> _vertices = new(); 
    
    public virtual void GenerateMesh(int resolution)
    {
        CleanUp();
    }

    protected virtual void CleanUp()
    {
        _vertices.Clear();
        _verticesPositions.Clear();
        _verticesNormals.Clear();
    }
}
