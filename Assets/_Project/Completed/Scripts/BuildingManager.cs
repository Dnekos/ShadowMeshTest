using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace InfallibleCode.Completed
{
    public class BuildingManager : MonoBehaviour
    {
        [SerializeField] private List<Building> buildings;
        
        private BuildingUpdateJob _job;
        private NativeArray<Building.Data> _buildingDataArray;

        private void Awake()
        {
            var buildingData = new Building.Data[buildings.Count];
            for (var i = 0; i < buildingData.Length; i++)
            {
                buildingData[i] = new Building.Data(buildings[i]);
            }
           
            _buildingDataArray = new NativeArray<Building.Data>(buildingData, Allocator.Persistent);
            
            _job = new BuildingUpdateJob
            {
                BuildingDataArray = _buildingDataArray
            };
        }

        private void Update()
        {
            var jobHandle = _job.Schedule(buildings.Count, 1);
            jobHandle.Complete();
        }

        private void OnDestroy()
        {
            _buildingDataArray.Dispose();
        }
    }
}
public struct TriangulationData
{
    public Vector3 normvec;
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Node> nodes;
    public int resolution;

    public void TriangulateCellRows()
    {
        Debug.Log("nodecount: " + nodes.Count + ", reso: " + resolution);
        int cells = resolution - 1;
        for (int i = 0, y = 0; y < cells; y++, i++)
        {
            for (int x = 0; x < cells; x++, i++)
            {
                TriangulateCell(
                    nodes[i],
                    nodes[i + 1],
                    nodes[i + resolution],
                    nodes[i + resolution + 1],
                    x, y); //x y needed for walls at edge
            }

        }
    }

    Vector3 AvgNodes(Node a, Node b)
    {
        return (a.position + b.position) * 0.5f;
    }

    private void TriangulateCell(Node a, Node b, Node c, Node d, int x, int y)
    {
        int cellType = 0;
        if (a.filled)
            cellType |= 1;
        if (b.filled)
            cellType |= 2;
        if (c.filled)
            cellType |= 4;
        if (d.filled)
            cellType |= 8;

        switch (cellType)
        {
            case 0:
                return;
            //single triangle
            case 1:
                AddTriangle(a.position, AvgNodes(a, c), AvgNodes(a, b)); // main mesh

                AddTriangle(a.position + normvec, AvgNodes(a, b) + normvec, AvgNodes(a, c) + normvec); // opposite end mesh
                AddQuad(AvgNodes(a, c), AvgNodes(a, c) + normvec, AvgNodes(a, b) + normvec, AvgNodes(a, b)); // side mesh
                break;
            case 2:
                AddTriangle(b.position, AvgNodes(a, b), AvgNodes(b, d));

                AddTriangle(b.position + normvec, AvgNodes(b, d) + normvec, AvgNodes(a, b) + normvec);
                AddQuad(AvgNodes(a, b), AvgNodes(a, b) + normvec, AvgNodes(b, d) + normvec, AvgNodes(b, d));
                break;
            case 4:
                AddTriangle(c.position, AvgNodes(c, d), AvgNodes(a, c));

                AddTriangle(c.position + normvec, AvgNodes(a, c) + normvec, AvgNodes(c, d) + normvec);
                AddQuad(AvgNodes(c, d), AvgNodes(c, d) + normvec, AvgNodes(a, c) + normvec, AvgNodes(a, c));
                break;
            case 8:
                AddTriangle(d.position, AvgNodes(b, d), AvgNodes(c, d));

                AddTriangle(d.position + normvec, AvgNodes(c, d) + normvec, AvgNodes(b, d) + normvec);
                AddQuad(AvgNodes(b, d), AvgNodes(b, d) + normvec, AvgNodes(c, d) + normvec, AvgNodes(c, d));
                break;
            //quad
            case 3:
                AddQuad(a.position, AvgNodes(a, c), AvgNodes(b, d), b.position);

                AddQuad(a.position + normvec, b.position + normvec, AvgNodes(b, d) + normvec, AvgNodes(a, c) + normvec);
                AddQuad(AvgNodes(a, c), AvgNodes(a, c) + normvec, AvgNodes(b, d) + normvec, AvgNodes(b, d));
                break;
            case 5:
                AddQuad(a.position, c.position, AvgNodes(c, d), AvgNodes(a, b));

                AddQuad(a.position + normvec, AvgNodes(a, b) + normvec, AvgNodes(c, d) + normvec, c.position + normvec);
                AddQuad(AvgNodes(c, d), AvgNodes(c, d) + normvec, AvgNodes(a, b) + normvec, AvgNodes(a, b));
                break;
            case 10:
                AddQuad(AvgNodes(a, b), AvgNodes(c, d), d.position, b.position);

                AddQuad(AvgNodes(a, b) + normvec, b.position + normvec, d.position + normvec, AvgNodes(c, d) + normvec);
                AddQuad(AvgNodes(a, b), AvgNodes(a, b) + normvec, AvgNodes(c, d) + normvec, AvgNodes(c, d));
                break;
            case 12:
                AddQuad(AvgNodes(a, c), c.position, d.position, AvgNodes(b, d));

                AddQuad(AvgNodes(a, c) + normvec, AvgNodes(b, d) + normvec, d.position + normvec, c.position + normvec);
                AddQuad(AvgNodes(a, c), AvgNodes(a, c) + normvec, AvgNodes(b, d) + normvec, AvgNodes(b, d));
                break;
            case 15:
                AddQuad(a.position, c.position, d.position, b.position);

                AddQuad(a.position + normvec, b.position + normvec, d.position + normvec, c.position + normvec);

                if (x == 0)
                    AddQuad(a.position, a.position + normvec, c.position + normvec, c.position);
                else if (x == resolution - 2)
                    AddQuad(b.position, d.position, d.position + normvec, b.position + normvec);
                if (y == 0)
                    AddQuad(a.position, b.position, b.position + normvec, a.position + normvec);
                else if (y == resolution - 2)
                    AddQuad(d.position, c.position, c.position + normvec, d.position + normvec);

                break;
            //pentagon
            case 7:
                AddPentagon(a.position, c.position, AvgNodes(c, d), AvgNodes(b, d), b.position);

                AddPentagon(a.position + normvec, b.position + normvec, AvgNodes(b, d) + normvec, AvgNodes(c, d) + normvec, c.position + normvec);
                AddQuad(AvgNodes(c, d), AvgNodes(c, d) + normvec, AvgNodes(b, d) + normvec, AvgNodes(b, d));
                break;
            case 11:
                AddPentagon(b.position, a.position, AvgNodes(a, c), AvgNodes(c, d), d.position);

                AddPentagon(b.position + normvec, d.position + normvec, AvgNodes(c, d) + normvec, AvgNodes(a, c) + normvec, a.position + normvec);
                AddQuad(AvgNodes(a, c), AvgNodes(a, c) + normvec, AvgNodes(c, d) + normvec, AvgNodes(c, d));
                break;
            case 13:
                AddPentagon(c.position, d.position, AvgNodes(b, d), AvgNodes(a, b), a.position);

                AddPentagon(c.position + normvec, a.position + normvec, AvgNodes(a, b) + normvec, AvgNodes(b, d) + normvec, d.position + normvec);
                AddQuad(AvgNodes(b, d), AvgNodes(b, d) + normvec, AvgNodes(a, b) + normvec, AvgNodes(a, b));
                break;
            case 14:
                AddPentagon(d.position, b.position, AvgNodes(a, b), AvgNodes(a, c), c.position);

                AddPentagon(d.position + normvec, c.position + normvec, AvgNodes(a, c) + normvec, AvgNodes(a, b) + normvec, b.position + normvec);
                AddQuad(AvgNodes(a, b), AvgNodes(a, b) + normvec, AvgNodes(a, c) + normvec, AvgNodes(a, c));
                break;
            //opposite corner
            case 6:
                AddTriangle(b.position, AvgNodes(a, b), AvgNodes(b, d));
                AddTriangle(c.position, AvgNodes(c, d), AvgNodes(a, c));

                AddTriangle(b.position + normvec, AvgNodes(b, d) + normvec, AvgNodes(a, b) + normvec);
                AddTriangle(c.position + normvec, AvgNodes(a, c) + normvec, AvgNodes(c, d) + normvec);
                AddQuad(AvgNodes(a, b), AvgNodes(a, b) + normvec, AvgNodes(b, d) + normvec, AvgNodes(b, d));
                AddQuad(AvgNodes(c, d), AvgNodes(c, d) + normvec, AvgNodes(a, c) + normvec, AvgNodes(a, c));
                break;
            case 9:
                AddTriangle(a.position, AvgNodes(a, c), AvgNodes(a, b));
                AddTriangle(d.position, AvgNodes(b, d), AvgNodes(c, d));

                AddTriangle(a.position + normvec, AvgNodes(a, b) + normvec, AvgNodes(a, c) + normvec);
                AddTriangle(d.position + normvec, AvgNodes(c, d) + normvec, AvgNodes(b, d) + normvec);
                AddQuad(AvgNodes(a, c), AvgNodes(a, c) + normvec, AvgNodes(a, b), AvgNodes(a, b) + normvec);
                AddQuad(AvgNodes(b, d), AvgNodes(b, d) + normvec, AvgNodes(c, d) + normvec, AvgNodes(c, d));
                break;
        }
    }

    //checks if point exists in vertices, if so returns index, if not, adds and returns new index
    private int CheckAdd(Vector3 point)
    {
        int index = vertices.IndexOf(point);
        if (index != -1)
            return index;
        vertices.Add(point);
        return vertices.Count - 1;
    }
    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int indexa = CheckAdd(a),
            indexb = CheckAdd(b),
            indexc = CheckAdd(c),
            indexd = CheckAdd(d);
        triangles.Add(indexa);
        triangles.Add(indexb);
        triangles.Add(indexc);
        triangles.Add(indexa);
        triangles.Add(indexc);
        triangles.Add(indexd);
    }
    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        triangles.Add(CheckAdd(a));
        triangles.Add(CheckAdd(b));
        triangles.Add(CheckAdd(c));
    }
    private void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
    {
        int vertexIndex = vertices.Count;
        int indexa = CheckAdd(a),
            indexb = CheckAdd(b),
            indexc = CheckAdd(c),
            indexd = CheckAdd(d),
            indexe = CheckAdd(e);

        triangles.Add(indexa);
        triangles.Add(indexb);
        triangles.Add(indexc);
        triangles.Add(indexa);
        triangles.Add(indexc);
        triangles.Add(indexd);
        triangles.Add(indexa);
        triangles.Add(indexd);
        triangles.Add(indexe);
    }
}