using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralPlanet : MonoBehaviour
{
    [Flags]
    public enum GizmoMode { Nothing = 0, Vertices = 1, Normals = 0b10, Tangents = 0b100 }
    
    [SerializeField] private GizmoMode _gizmos;

    [Range(1, 6)]
    [SerializeField] private int _resolution;

    [SerializeField] private Material _material;
   
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    private readonly List<Vertex> _vertices = new();

    private readonly Dictionary<long, int> _middlePointIndexCache = new();
    private int _vertexIndex;
    
    private Vector3[] gizmoVertices;
    private Vector3[] gizmoNormals;
    
    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh
        {
            name = "Procedural Planet"
        };
            
        GenerateMesh();
        _meshFilter.mesh = _mesh;
    }

    private void OnValidate()
    {
        if (_mesh == null) return;
        CleanUp();
        
        GenerateMesh();
    }

    private void CleanUp()
    {
        gizmoVertices = null;
        gizmoNormals = null;
        
        _vertexIndex = 0;
        _vertices.Clear();
        _middlePointIndexCache.Clear();
        _mesh.Clear();
    }

    private void GenerateMesh()
    {
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
        var triangles = new List<Triangle>
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

        for (int i = 1; i < _resolution; i++)
        {
            var newTriangles = new List<Triangle>();

            foreach (var tri in triangles)
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
                
            triangles = newTriangles;
        }

        var trianglesArray = new int[triangles.Count * 3];
        int index = 0;
        // done, now add triangles to mesh
        for (var i = 0; i < triangles.Count; i++)
        {
            trianglesArray[index] = triangles[i].a;
            index++;
            trianglesArray[index] = triangles[i].b;
            index++;
            trianglesArray[index] = triangles[i].c;
            index++;
        }

        List<Vector3> verts = new List<Vector3>(_vertices.Count);
        List<Vector3> norms = new List<Vector3>(_vertices.Count);
        
        _vertices.ForEach(v =>
        {
            verts.Add(v.Position);
            norms.Add(v.Normal);
        });
        
        _mesh.SetVertices(verts.ToArray());
        _mesh.SetNormals(norms.ToArray());
        _mesh.SetTriangles(trianglesArray, 0);
        _meshRenderer.material = _material;
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

    private void OnDrawGizmos()
    {
        if (_gizmos == GizmoMode.Nothing || _mesh == null) return;
        
        bool drawVertices = (_gizmos & GizmoMode.Vertices) != 0;
        bool drawNormals = (_gizmos & GizmoMode.Normals) != 0;
        
        if (gizmoVertices == null)
            gizmoVertices = _mesh.vertices;
        
        if (drawNormals && gizmoNormals == null)
            gizmoNormals = _mesh.normals;

        Transform t = transform;
        
        for (var i = 0; i < gizmoVertices.Length; i++)
        {
            Vector3 position = t.TransformPoint(gizmoVertices[i]);
            
            if (drawVertices)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(position, 0.02f);
            }
            
            if (drawNormals) 
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(position, t.TransformDirection(gizmoNormals[i]) * 0.2f);
            }
        }
    }
}