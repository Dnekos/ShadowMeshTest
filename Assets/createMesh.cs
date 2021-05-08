using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System.Threading;
using UnityEngine.Jobs;
public class Node
{
    public Vector3 position;
    public bool filled;

    public Node(Vector3 pos, float size, bool state)
    {
        position = pos;
        filled = state;
    }
}

public class createMesh : MonoBehaviour
{


    public Material mat;
    public GameObject shadow_prefab;

    private Mesh mesh;

    private List<Vector3> vertices;
    private List<int> triangles;

    public int resolution = 10;
    public float radius = 1;
    public List<Node> nodes;

    // node creation
    Vector3 normvec;
    Vector3 sp;
    RaycastHit hitinfo;

    private Light[] lightsources;

    private void Start()
    {
        //grabs list of Lights for isShadow
        lightsources = GameObject.FindObjectsOfType<Light>();
    }

    public void CreateShadowMesh()
    {
        mesh = new Mesh();
        mesh.name = "Shadow Mesh";
        vertices = new List<Vector3>();
        triangles = new List<int>();
        nodes = new List<Node>();
        getListofPoints();
    }

    //https://catlikecoding.com/unity/tutorials/marching-squares/
    private void Triangulate()
    {
        vertices.Clear();
        triangles.Clear();
        mesh.Clear();

        TriangulateCellRows();

        if (vertices.Count != 0)
        {
            //fix for inverted shadows 
            if (normvec == Vector3.down || normvec == Vector3.forward || normvec == Vector3.right)
                ReverseFaces();


            //idk if this normal thing is working
            //Vector3[] normarray = new Vector3[vertices.Count];
            //for (int i = 0; i < vertices.Count; i++)
            //    normarray[i] = normvec;

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            //mesh.normals = normarray;
            mesh.RecalculateNormals();

            shadow_prefab.GetComponent<MeshFilter>().mesh = mesh;
            shadow_prefab.GetComponent<MeshRenderer>().material = mat;
            shadow_prefab.GetComponent<MeshCollider>().sharedMesh = mesh;
            shadow_prefab.GetComponent<ShadowScript>().NV = normvec;

            Instantiate(shadow_prefab);
        }
    }

    private void ReverseFaces()
    {
        for (int i = 1; i < triangles.Count; i += 3)
        {
            int temp = triangles[i];
            triangles[i] = triangles[i + 1];
            triangles[i + 1] = temp;
        }
    }

    private void TriangulateCellRows()
    {
        Debug.Log("nodecount: " + nodes.Count+", reso: "+resolution);
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
                    x,y); //x y needed for walls at edge
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

        Debug.Log(cellType);

        switch (cellType)
        {
            case 0:
                return;
                //single triangle
            case 1:
                AddTriangle(a.position, AvgNodes(a,c), AvgNodes(a, b)); // main mesh

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
                AddQuad(AvgNodes(b, d),  AvgNodes(b, d) + normvec, AvgNodes(c, d) + normvec,AvgNodes(c, d));
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
                AddQuad(AvgNodes(a, b),AvgNodes(a, b) + normvec,  AvgNodes(c, d) + normvec, AvgNodes(c, d));
                break;
            case 12:
                AddQuad(AvgNodes(a, c), c.position, d.position, AvgNodes(b, d));

                AddQuad(AvgNodes(a, c) + normvec, AvgNodes(b, d) + normvec, d.position + normvec, c.position + normvec);
                AddQuad(AvgNodes(a, c),AvgNodes(a, c) + normvec,  AvgNodes(b, d) + normvec, AvgNodes(b, d));
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
                AddQuad(AvgNodes(c, d), AvgNodes(c, d) + normvec,AvgNodes(b, d) + normvec,  AvgNodes(b, d));
                break;
            case 11:
                AddPentagon(b.position, a.position, AvgNodes(a, c), AvgNodes(c, d), d.position);

                AddPentagon(b.position + normvec, d.position + normvec, AvgNodes(c, d) + normvec, AvgNodes(a, c) + normvec, a.position + normvec);
                AddQuad(AvgNodes(a, c), AvgNodes(a, c) + normvec, AvgNodes(c, d) + normvec, AvgNodes(c, d));
                break;
            case 13:
                AddPentagon(c.position, d.position, AvgNodes(b, d), AvgNodes(a, b), a.position);

                AddPentagon(c.position + normvec, a.position + normvec, AvgNodes(a, b) + normvec, AvgNodes(b, d) + normvec, d.position + normvec);
                AddQuad(AvgNodes(b, d), AvgNodes(b, d) + normvec,AvgNodes(a, b) + normvec, AvgNodes(a, b) );
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
                AddQuad(AvgNodes(a, c), AvgNodes(a, c) + normvec,  AvgNodes(a, b), AvgNodes(a, b) + normvec);
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
    bool isShadow(Vector3 point)
    {
        // check if the selected point is a shadow, abort if it has line of sight with any lights
        RaycastHit info;
        foreach (Light light in lightsources)
            if (Physics.Raycast(point, light.transform.position - point, out info, 50f))
                if (info.transform.tag == "Light")
                    return false;
        return true;
    }
    void reticalRaycast()
    {
        Camera cam = gameObject.GetComponent<Camera>();
        Vector3 midpoint = new Vector3(cam.scaledPixelWidth / 2, cam.scaledPixelHeight / 2, 0);
        Ray midray = cam.ScreenPointToRay(midpoint);
        Physics.Raycast(midray, out hitinfo);
        Debug.DrawRay(midray.origin, midray.direction *100f,Color.red,1f); //Debug.Log(midray.origin + ", " + midray.direction);
    }

    void getListofPoints( )
    {
        Camera cam = gameObject.GetComponent<Camera>();
        reticalRaycast();


        normvec = hitinfo.normal;
        sp = Vector3.one - new Vector3(Mathf.Abs(normvec.x), Mathf.Abs(normvec.y), Mathf.Abs(normvec.z)); // surfaceparallel
        shadow_prefab.transform.position = hitinfo.point;

        float start = Time.realtimeSinceStartup;
        CreateNodes();
        Debug.Log("Creating the nodes themselves took: " + (Time.realtimeSinceStartup - start));
        /*
        //get the current view
        Texture2D view = getImage(cam);
        Color32[] colors = view.GetPixels32();
        resolution = view.width;
        */
        Triangulate();
    }

    void CreateNodes()
    {
        Vector3 point = hitinfo.point;
        float size = 2 * radius / resolution;

        // adds up to (2*radius)^3 vertexes if on surface
        for (float x = -radius * sp.x; x < (radius - size) * sp.x + size; x += size)
            for (float y = -radius * sp.y; y < (radius - size) * sp.y + size; y += size)
                for (float z = -radius * sp.z; z < (radius - size) * sp.z + size; z += size)
                {
                    Vector3 pos = new Vector3(point.x + x, point.y + y, point.z + z);

                    bool state = (hitinfo.collider.ClosestPointOnBounds(pos) == pos && isShadow(pos));
                    if (state)
                        Debug.Log("making filled node");
                    else
                        Debug.Log("making empty node");

                    nodes.Add(new Node(pos - point, size, state));
                }
    }
    Texture2D getImage(Camera cam)
    {
        // The Render Texture in RenderTexture.active is the one
        // that will be read by ReadPixels.
        var currentRT = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;

        // Render the camera's view.
        cam.Render();

        // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        // Replace the original active Render Texture.
        RenderTexture.active = currentRT;
        return image;
    }

    // Start is called before the first frame update
    void MeshTest()
    {
        Vector3[] vertices = new Vector3[8];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        vertices[0] = new Vector3(0, 1, 0);
        vertices[1] = new Vector3(1, 1, 0);
        vertices[2] = new Vector3(0, 0, 0);
        vertices[3] = new Vector3(1, 0, 0);
        vertices[4] = new Vector3(0, 1, 1);
        vertices[5] = new Vector3(1, 1, 1);
        vertices[6] = new Vector3(0, 0, 1);
        vertices[7] = new Vector3(1, 0, 1);

        uv[0] = new Vector2(0, 1);
        uv[1] = new Vector2(1,1);
        uv[2] = new Vector2(0,0);
        uv[3] = new Vector2(1,0);


        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        //mesh.uv = uv;
        mesh.triangles = triangles;

        GameObject obj = new GameObject("Mesh", typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.localScale = new Vector3(1, 1, 1);

        obj.GetComponent<MeshFilter>().mesh = mesh;
        obj.GetComponent<MeshRenderer>().material = mat;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CreateShadowMesh();
            //RaycastHit hit = reticalRaycast();
            //Debug.Log(isShadow(hit.point));
        }
    }
}
