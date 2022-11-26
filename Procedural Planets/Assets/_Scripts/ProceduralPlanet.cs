using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralPlanet : MonoBehaviour
{
    [Flags]
    public enum GizmoMode { Nothing = 0, Vertices = 1, Normals = 0b10, Tangents = 0b100 }
    
    [SerializeField] private GizmoMode _gizmos;

    [SerializeField] private eSphereType _sphereType;
    
    [Range(1, 6)]
    [SerializeField] private int _resolution;

    [SerializeField] private Material _material;
   
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    
    private SphereMeshGenerator _meshGenerator;
    private readonly Dictionary<eSphereType, SphereMeshGenerator> SphereMeshGenerators = new()
    {
        { eSphereType.CubeSphere, new CubeSphereGenerator() },
        { eSphereType.Icosphere, new IcoSphereGenerator()}
    };
    
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
        
        _mesh.Clear();
    }

    private void GenerateMesh()
    {
        _meshGenerator = SphereMeshGenerators[_sphereType];
        _meshGenerator.GenerateMesh(_resolution);
        
        _mesh.SetVertices(_meshGenerator.VerticesPositions);
        _mesh.SetNormals(_meshGenerator.VerticesNormals);
        _mesh.SetTriangles(_meshGenerator.Triangles, 0);
        _meshRenderer.material = _material;
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

public enum eSphereType
{
    CubeSphere,
    Icosphere
}