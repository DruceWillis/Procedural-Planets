using System;
using System.Collections.Generic;
using UnityEngine;

public class IcoSphereGenerator : SphereMeshGenerator
{
    private readonly Dictionary<long, int> _middlePointIndexCache = new();
    private int _vertexIndex;

    protected override void CleanUp()
    {
        base.CleanUp();
        _vertexIndex = 0;
        _middlePointIndexCache.Clear();
    }

    public override void GenerateMesh(int resolution)
    {
        base.GenerateMesh(resolution);
        
        float t = (float)((1.0 + Math.Sqrt(5.0)) / 2.0);
        
        AddVertex(new Vector3(-1,  t,  0));
        AddVertex(new Vector3( 1,  t,  0));
        AddVertex(new Vector3(-1, -t,  0));
        AddVertex(new Vector3( 1, -t,  0));
        AddVertex(new Vector3( 0, -1,  t));
        AddVertex(new Vector3( 0,  1,  t));
        AddVertex(new Vector3( 0, -1, -t));
        AddVertex(new Vector3( 0,  1, -t));
        AddVertex(new Vector3( t,  0, -1));
        AddVertex(new Vector3( t,  0,  1));
        AddVertex(new Vector3(-t,  0, -1));
        AddVertex(new Vector3(-t,  0,  1));

        // create 20 triangles of the icosahedron
        var trianglesList = new List<Triangle>
        {
            new(0, 11, 5),
            new(0, 5, 1),
            new(0, 1, 7),
            new(0, 7, 10),
            new(0, 10, 11),
            new(1, 5, 9),
            new(5, 11, 4),
            new(11, 10, 2),
            new(10, 7, 6),
            new(7, 1, 8),
            new(3, 9, 4),
            new(3, 4, 2),
            new(3, 2, 6),
            new(3, 6, 8),
            new(3, 8, 9),
            new(4, 9, 5),
            new(2, 4, 11),
            new(6, 2, 10),
            new(8, 6, 7),
            new(9, 8, 1)
        };

        for (int i = 1; i < resolution; i++)
        {
            var newTriangles = new List<Triangle>();

            foreach (var tri in trianglesList)
            {
                // replace triangle by 4 triangles
                int a = GetMiddlePoint(tri.a, tri.b);
                int b = GetMiddlePoint(tri.b, tri.c);
                int c = GetMiddlePoint(tri.c, tri.a);
                
                newTriangles.Add(new Triangle(tri.a, a, c));
                newTriangles.Add(new Triangle(tri.b, b, a));
                newTriangles.Add(new Triangle(tri.c, c, b));
                newTriangles.Add(new Triangle(a, b, c));
            }
                
            trianglesList = newTriangles;
        }

        _vertices.ForEach(v =>
        {
            _verticesPositions.Add(v.Position);
            _verticesNormals.Add(v.Normal);
        });
        
        _triangles = new int[trianglesList.Count * 3];
        int index = 0;
       
        for (var i = 0; i < trianglesList.Count; i++)
        {
            _triangles[index] = trianglesList[i].a;
            index++;
            _triangles[index] = trianglesList[i].b;
            index++;
            _triangles[index] = trianglesList[i].c;
            index++;
        }
    }
    
    private int AddVertex(Vector3 position)
    {
        float length = (float)Math.Sqrt(position.x * position.x + position.y * position.y + position.z * position.z);
        var p = new Vector3(position.x / length, position.y / length, position.z / length);
        _vertices.Add(new Vertex{Position = p, Normal = p});
        return _vertexIndex++;
    }

    private int GetMiddlePoint(int p1, int p2)
    {
        bool firstIsSmaller = p1 < p2;
        long smallerIndex = firstIsSmaller ? p1 : p2;
        long greaterIndex = firstIsSmaller ? p2 : p1;
        long key = (smallerIndex << 32) + greaterIndex;

        if (_middlePointIndexCache.TryGetValue(key, out int middlePoint))
        {
            return middlePoint;
        }
        
        Vector3 point1 = _vertices[p1].Position;
        Vector3 point2 = _vertices[p2].Position;
        Vector3 middle = new Vector3(
            (point1.x + point2.x) * 0.5f, 
            (point1.y + point2.y) * 0.5f, 
            (point1.z + point2.z) * 0.5f);

        // add vertex makes sure point is on unit sphere
        int i = AddVertex(middle); 

        // store it, return index
        _middlePointIndexCache.Add(key, i);
        return i;
    }
}
