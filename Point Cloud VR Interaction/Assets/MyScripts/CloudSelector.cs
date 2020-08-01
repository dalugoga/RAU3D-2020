using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PointCloudSegmentation;

public class CloudSelector : MonoBehaviour
{
    VRTK.VRTK_StraightPointerRenderer pointer_renderer;
    VRTK.VRTK_ControllerEvents right_controller_events;
    VRTK.VRTK_ControllerEvents left_controller_events;

    GameObject selected_cloud = null;

    public Color old_color;
    public Color new_color = new Color(255 / 255.0f, 61 / 255.0f, 249 / 255.0f);
    public Color selected_color;
    public Color edit_color;

    private Vector3 scaleChange = new Vector3(-0.05f, -0.05f, -0.05f);

    public Material default_material;
    public Material selected_material;
    public Material selected_back_material;

    public Material select_tool_material;
    public Material unselect_tool_material;

    GameObject select_bubble;
    MeshRenderer select_bubble_renderer;

    HashSet<int> selected_vertices;
    HashSet<int> segmented_vertices;
    Vector3[] point_cloud_vertices;
    Color[] point_cloud_colors;

    Vector3 grab_start_pos;
    Vector3 last_grab_pos;
    Quaternion last_grab_rot;
    GameObject selected_go;

    public GameObject segment_canvas;

    enum tool_state
    {
        Inactive,
        Select,
        Unselect
    }

    tool_state tool = tool_state.Inactive;

    // Start is called before the first frame update
    void Start()
    {
        pointer_renderer = GameObject.Find("RightControllerScriptAlias").GetComponent<VRTK.VRTK_StraightPointerRenderer>();
        right_controller_events = GameObject.Find("RightControllerScriptAlias").GetComponent<VRTK.VRTK_ControllerEvents>();
        left_controller_events = GameObject.Find("LeftControllerScriptAlias").GetComponent<VRTK.VRTK_ControllerEvents>();
        select_bubble = GameObject.Find("Select Bubble");
        select_bubble_renderer = select_bubble.GetComponent<MeshRenderer>();
        select_bubble_renderer.enabled = false;
        selected_vertices = new HashSet<int>();
        segmented_vertices = new HashSet<int>();

        right_controller_events.TriggerPressed += new VRTK.ControllerInteractionEventHandler(DoTriggerPressed);
        right_controller_events.ButtonTwoPressed += new VRTK.ControllerInteractionEventHandler(DoButtonTwoPressed);
        right_controller_events.GripPressed += new VRTK.ControllerInteractionEventHandler(DoGripPressed);
        right_controller_events.GripReleased += new VRTK.ControllerInteractionEventHandler(DoGripReleased);
        left_controller_events.GripPressed += new VRTK.ControllerInteractionEventHandler(DoGripPressedLeft);
    }

    private void Update()
    {
        if (right_controller_events.gripPressed)
        {
            Vector3 controller_pos = VRTK.VRTK_DeviceFinder.GetControllerRightHand().transform.position;
            Quaternion controller_rot = VRTK.VRTK_DeviceFinder.GetControllerLeftHand().transform.rotation;
            selected_go.transform.position = selected_go.transform.position + (controller_pos - last_grab_pos);

            last_grab_pos = controller_pos;

            if (left_controller_events.gripPressed)
            {
                selected_go.transform.rotation = (controller_rot * Quaternion.Inverse(last_grab_rot)) * selected_go.transform.rotation;
                last_grab_rot = controller_rot;
                
            }
        }
    }

    public void InstantiateSelectedGameObject()
    {
        Matrix4x4 localToWorld = selected_cloud.transform.localToWorldMatrix;

        Mesh mesh = new Mesh();
        Color[] colors = new Color[selected_vertices.Count];
        Vector3[] vertices = new Vector3[selected_vertices.Count];
        int[] triangles = new int[selected_vertices.Count * 3];

        int i = 0;
        foreach (int vert in selected_vertices)
        {
            colors[i] = edit_color;
            vertices[i] = localToWorld.MultiplyPoint3x4(point_cloud_vertices[vert]);
            triangles[i * 3 + 0] = i;
            triangles[i * 3 + 1] = i;
            triangles[i * 3 + 2] = i;
            i++;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateBounds();

        Vector3 mesh_center = mesh.bounds.center;

        for(int k = 0; k < vertices.Length; k++)
            vertices[k] = vertices[k] + (Vector3.zero - mesh_center);

        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        selected_go = new GameObject("Edit Point Cloud");
        selected_go.AddComponent<MeshFilter>();
        selected_go.AddComponent<MeshRenderer>();
        selected_go.GetComponent<Renderer>().materials = new Material[] { selected_material, selected_back_material };
        selected_go.GetComponent<Renderer>().material = selected_material;
        selected_go.GetComponent<MeshFilter>().mesh = mesh;
        selected_go.transform.position = mesh_center;
    }

    void DoGripPressed(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        grab_start_pos = e.controllerReference.actual.transform.position;
        last_grab_pos = e.controllerReference.actual.transform.position;

        InstantiateSelectedGameObject();
    }

    void DoGripPressedLeft(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        last_grab_rot = VRTK.VRTK_DeviceFinder.GetControllerLeftHand().transform.rotation;
    }

    void DoGripReleased(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        Vector3 end_start_pos = e.controllerReference.actual.transform.position;
        Quaternion end_start_rot = selected_go.transform.rotation;
        Vector3 translate_vector = end_start_pos - grab_start_pos;

        Mesh mesh = selected_cloud.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = new Vector3[point_cloud_vertices.Length];

        Matrix4x4 worldToLocal = selected_cloud.transform.worldToLocalMatrix;
        Vector3 mesh_center = worldToLocal.MultiplyPoint3x4(selected_go.transform.position);

        for (int i = 0; i < vertices.Length; i++)
        {
            if (selected_vertices.Contains(i))
            {
                Vector3 translate_vert = point_cloud_vertices[i] + translate_vector;
                Vector3 rotate_vert = end_start_rot * (translate_vert - mesh_center) + mesh_center;
                vertices[i] = rotate_vert;
            }
            else
                vertices[i] = point_cloud_vertices[i];
        }
        
        point_cloud_vertices = vertices;
        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        Mesh surface = selected_cloud.transform.GetChild(0).gameObject.GetComponent<MeshFilter>().mesh;
        Mesh surface_inv = selected_cloud.transform.GetChild(0).GetChild(0).gameObject.GetComponent<MeshFilter>().mesh;

        surface.vertices = vertices;
        surface_inv.vertices = vertices;

        Destroy(selected_go);
        selected_go = null;
    }

    void DoTriggerPressed(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        if (pointer_renderer.IsVisible() && pointer_renderer.GetDestinationHit().collider != null && pointer_renderer.GetDestinationHit().collider.gameObject.tag == "Point Cloud")
        {
            if (selected_cloud != null && selected_cloud.name == pointer_renderer.GetDestinationHit().collider.gameObject.name)
                UnselectCloud(selected_cloud);
            else
                SelectNewCloud(pointer_renderer.GetDestinationHit().collider.gameObject);
        }
    }

    public void SelectNewCloud(GameObject go)
    {
        Material[] matArray = new Material[2];
        matArray[0] = selected_material;
        matArray[1] = selected_back_material;

        if (selected_cloud != null)
            UnselectCloud(selected_cloud);

        selected_cloud = go;

        go.GetComponent<Renderer>().materials = matArray;

        point_cloud_vertices = new Vector3[selected_cloud.GetComponent<MeshFilter>().mesh.vertices.Length];
        System.Array.Copy(selected_cloud.GetComponent<MeshFilter>().mesh.vertices, point_cloud_vertices, point_cloud_vertices.Length);

        point_cloud_colors = new Color[selected_cloud.GetComponent<MeshFilter>().mesh.colors.Length];
        System.Array.Copy(selected_cloud.GetComponent<MeshFilter>().mesh.colors, point_cloud_colors, point_cloud_colors.Length);

        UpdateColor(selected_cloud, new_color, true);

        PointCloudSegmentation segmentation = go.GetComponent<PointCloudSegmentation>();
        Dropdown dropdown = segment_canvas.transform.Find("Dropdown Classes").GetComponent<Dropdown>();

        if (segmentation == null)
            go.AddComponent<PointCloudSegmentation>();
        else
        {
            foreach (PointCloudSegment pcs in segmentation.GetSegments())
                Segmentation(pcs.segment_points, pcs.segment_class.class_color);

            foreach (PointCloudSegmentClass pcc in segmentation.GetClasses())
                dropdown.AddOptions(new List<string>() { pcc.class_name });
        }
    }

    public void UnselectCloud(GameObject go)
    {
        Material[] matArray = new Material[1];
        matArray[0] = default_material;

        UpdateColor(go, old_color, false);
        go.GetComponent<Renderer>().materials = matArray;
        selected_cloud = null;
        point_cloud_vertices = null;
        selected_vertices.Clear();
        segmented_vertices.Clear();
        segment_canvas.transform.Find("Dropdown Classes").GetComponent<Dropdown>().ClearOptions();
        //GameObject.Find("Dropdown Classes").GetComponent<Dropdown>().ClearOptions();
    }

    void DoButtonTwoPressed(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        Color paint_color;

        if (tool == tool_state.Inactive)
            return;
        else if (tool == tool_state.Select)
            paint_color = selected_color;
        else
            paint_color = new_color;

        float bubble_radius = select_bubble.GetComponent<SphereCollider>().radius * select_bubble.transform.localScale.x;
        Vector3 bubble_center = select_bubble.GetComponent<Renderer>().bounds.center;

        Mesh mesh = selected_cloud.GetComponent<MeshFilter>().mesh;
        Matrix4x4 localToWorld = selected_cloud.transform.localToWorldMatrix;

        //Color[] colors = new Color[point_cloud_colors.Length];
        float bubble_radius_squared = bubble_radius * bubble_radius;

        for (int i = 0; i < point_cloud_vertices.Length; i++)
        {
            if (segmented_vertices.Contains(i))
                continue;

            Vector3 point_world = localToWorld.MultiplyPoint3x4(point_cloud_vertices[i]);

            if((bubble_center - point_world).sqrMagnitude < bubble_radius_squared)
            {
                point_cloud_colors[i] = paint_color;
                if (tool == tool_state.Select)
                    selected_vertices.Add(i);
                else
                    selected_vertices.Remove(i);
            }
            //else
                //colors[i] = point_cloud_colors[i];
        }

        //point_cloud_colors = colors;
        mesh.colors = point_cloud_colors;
    }

    public void UpdateColor(GameObject go, Color color, bool save_color)
    {
        Mesh mesh = go.GetComponent<MeshFilter>().mesh;

        if (save_color)
            old_color = mesh.colors[0];

        Color[] colors = new Color[point_cloud_colors.Length];

        for (int i = 0; i < colors.Length; i++)
            colors[i] = color;

        point_cloud_colors = colors;
        mesh.colors = colors;
    }

    public void Segmentation(List<int> points, Color color)
    {
        Mesh mesh = selected_cloud.GetComponent<MeshFilter>().mesh;

        foreach (int i in points)
        {
            point_cloud_colors[i] = color;
        }

        mesh.colors = point_cloud_colors;

        segmented_vertices.UnionWith(selected_vertices);
        selected_vertices.Clear();
    }

    #region Radial Menus

    public void IncreasePointSize()
    {
        selected_material.SetFloat("_PointSize", selected_material.GetFloat("_PointSize") + 0.003f);
        selected_back_material.SetFloat("_PointSize", selected_back_material.GetFloat("_PointSize") + 0.003f);
    }

    public void DecreasePointSize()
    {
        selected_material.SetFloat("_PointSize", selected_material.GetFloat("_PointSize") - 0.003f);
        selected_back_material.SetFloat("_PointSize", selected_back_material.GetFloat("_PointSize") - 0.003f); 
    }

    public void ShowSurface()
    {
        selected_cloud.transform.GetChild(0).gameObject.SetActive(!selected_cloud.transform.GetChild(0).gameObject.activeSelf);
    }

    public void SelectTool()
    {
        if (!select_bubble_renderer.enabled)
        {
            select_bubble_renderer.enabled = true;
            tool = tool_state.Select;
        }
        else if (tool == tool_state.Select)
        {
            select_bubble_renderer.enabled = false;
            tool = tool_state.Inactive;
        }
        else if (tool == tool_state.Unselect)
            tool = tool_state.Select;

        select_bubble.GetComponent<Renderer>().material = select_tool_material;
    }

    public void UnSelectTool()
    {
        if (!select_bubble_renderer.enabled)
        {
            select_bubble_renderer.enabled = true;
            tool = tool_state.Unselect;
        }
        else if (tool == tool_state.Unselect)
        {
            select_bubble_renderer.enabled = false;
            tool = tool_state.Inactive;
        }
        else if (tool == tool_state.Select)
            tool = tool_state.Unselect;

        select_bubble.GetComponent<Renderer>().material = unselect_tool_material;
    }

    public void SizeUp()
    {
        select_bubble.transform.localScale -= scaleChange;
    }

    public void SizeDown()
    {
        select_bubble.transform.localScale += scaleChange;
    }

    public void Segment()
    {
        if (segment_canvas.activeSelf)
            segment_canvas.SetActive(false);
        else if (selected_vertices.Count != 0)
            segment_canvas.SetActive(true);
    }

    #endregion

    #region GETS

    public GameObject GetSelectedCloud()
    {
        return selected_cloud;
    }

    public HashSet<int> GetSelectedVertices()
    {
        return selected_vertices;
    }

    #endregion
}
