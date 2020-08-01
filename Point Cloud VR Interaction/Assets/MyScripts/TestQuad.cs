using UnityEngine;

public class TestQuad : MonoBehaviour
{
    public float width = 1;
    public float height = 1;

    public Material mat;

    public void Start()
    {
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mat;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(0, 0, 0),
            new Vector3(width, 0, 0),
            new Vector3(0, height, 0),
            new Vector3(width, height, 0)
        };
        mesh.vertices = vertices;

        int[] tris = new int[12]
        {
            // lower left triangle
            0, 0, 0,
            // upper right triangle
            1,1,1,

            2,2,2,

            3,3,3
        };
        mesh.triangles = tris;

        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        //mesh.normals = normals;

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        //mesh.uv = uv;

        Color[] colors = new Color[4]
        {
            new Color(1,1,0),
            new Color(1,1,0),
            new Color(1,1,0),
            new Color(1,1,0)
        };
        mesh.colors = colors;

        meshFilter.mesh = mesh;
    }
}
