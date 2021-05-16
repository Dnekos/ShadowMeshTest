using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Jobs;

public struct Node
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
    NodeTriangulateJob job;
    JobHandle jobHandle;
    bool startedjob = false;

    public Material mat;
    public GameObject shadow_prefab;

    private Mesh mesh;

    private NativeList<Vector3> nlVertices;
    private NativeList<int> nlTriangles;

    public int resolution = 10;
    public float radius = 1;
    public NativeList<Node> nlNodes;

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
        reticalRaycast();
        if (hitinfo.collider == null || hitinfo.collider.tag != "Ground")
        {
            Debug.LogWarning("Did not hit anything");
            return;
        }

        startedjob = true;

        mesh = new Mesh();
        mesh.name = "Shadow Mesh";
        nlVertices = new NativeList<Vector3>(Allocator.Persistent);
        nlTriangles = new NativeList<int>(Allocator.Persistent);
        nlNodes = new NativeList<Node>(Allocator.Persistent);
        getListofPoints();
    }

    //https://catlikecoding.com/unity/tutorials/marching-squares/
    private void Triangulate()
    {
        NativeList<Vector3> jobVert = nlVertices;
        NativeList<int> jobTriangles = nlTriangles;
        NativeList<Node> jobNodes = nlNodes;
        /*nlVertices.Dispose(jobHandle);
        nlTriangles.Dispose(jobHandle);
        nlNodes.Dispose(jobHandle);
        */
        job = new NodeTriangulateJob()
        {
            normvec = normvec,
            vertices = jobVert,
            triangles = jobTriangles,
            nodes = jobNodes,
            resolution = resolution

        };
        jobHandle = job.Schedule();
    }

    private void ReverseFaces()
    {
        for (int i = 1; i < nlTriangles.Length; i += 3)
        {
            int temp = nlTriangles[i];
            nlTriangles[i] = nlTriangles[i + 1];
            nlTriangles[i + 1] = temp;
        }
    }

    bool isShadow(Vector3 point)
    {
        // check if the selected point is a shadow, abort if it has line of sight with any lights
        RaycastHit info;
        int mask = ~((1 << 10) | (1 << 9));
        foreach (Light light in lightsources)
            if (Physics.Raycast(point, light.transform.position - point, out info, 50f, mask))
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

                    nlNodes.Add(new Node(pos - point, size, state));
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
        if (Input.GetMouseButtonDown(0) && !startedjob)
        {
            float start = Time.realtimeSinceStartup;
            CreateShadowMesh();
            Debug.LogWarning(Time.realtimeSinceStartup - start);
            //RaycastHit hit = reticalRaycast();
            //Debug.Log(isShadow(hit.point));
        }
        if (startedjob && jobHandle.IsCompleted)
        {
            startedjob = false;
            InstantiateMesh();
        }

    }
    private void OnApplicationQuit()
    {
        if (startedjob)
        {
            job.vertices.Dispose();
            job.triangles.Dispose();
            job.nodes.Dispose();
        }
    }

    void InstantiateMesh()
    {
        normvec = job.normvec;
        resolution = job.resolution;

        if (job.vertices.Length != 0)
        {
            //fix for inverted shadows 
            if (normvec == Vector3.down || normvec == Vector3.forward || normvec == Vector3.right)
                ReverseFaces();


            //idk if this normal thing is working
            //Vector3[] normarray = new Vector3[vertices.Count];
            //for (int i = 0; i < vertices.Count; i++)
            //    normarray[i] = normvec;

            mesh.vertices = job.vertices.ToArray();
            mesh.triangles = job.triangles.ToArray();
            //mesh.normals = normarray;
            mesh.RecalculateNormals();

            shadow_prefab.GetComponent<MeshFilter>().mesh = mesh;
            shadow_prefab.GetComponent<MeshRenderer>().material = mat;
            shadow_prefab.GetComponent<MeshCollider>().sharedMesh = mesh;
            shadow_prefab.GetComponent<ShadowScript>().NV = normvec;

            Instantiate(shadow_prefab);

        }

        job.vertices.Dispose();
        job.triangles.Dispose();
        job.nodes.Dispose();
    }
}
