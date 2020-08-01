using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class PointCloudManager : MonoBehaviour {

	// File
	public string dataPath;
	private string filename;
	public Material matVertex;
	public Material matSurface;

	// GUI
	private float progress = 0;
	private string guiText;
	private bool loaded = false;

	// PointCloud
	private GameObject pointCloud;

	public float scale = 1;
	public bool invertYZ = false;
	public bool forceReload = false;

	public int numPoints;
	public int numPointGroups;
	private int limitPoints = 65000;

	private Vector3[] points;
	private Color[] colors;
	private Vector3 minValue;

    private Vector3 reverse_vector = new Vector3(-1, -1, -1);

    string[] class_names = { "Und", "Ground", "Building", "Post", "Und", "Car", "Vegetation"};
    Color[] class_colors = { new Color(), new Color(0, 80 / 255.0f, 80 / 255.0f), 
        new Color(30 / 255.0f, 144 / 255.0f, 255 / 255.0f), new Color(255 / 255.0f, 255 / 255.0f, 0), 
        new Color(), new Color(230 / 255.0f, 60 / 255.0f, 0), new Color(0, 128 / 255.0f, 0) };

    public class PLYDataLine
    {
        public Vector3 pos;
        public int obj_class;
        public int obj_id;

        public PLYDataLine(string data, bool invertYZ)
        {
            string[] data_split = data.Replace('.', ',').Trim().Split();

            if(invertYZ)
                this.pos = new Vector3(float.Parse(data_split[0]), float.Parse(data_split[1]), float.Parse(data_split[2]));
            else
                this.pos = new Vector3(float.Parse(data_split[0]), float.Parse(data_split[2]), float.Parse(data_split[1]));

            this.obj_class = int.Parse(data_split[3]);

            if (data_split[4] == "nan")
                this.obj_id = 0;
            else
                this.obj_id = int.Parse(data_split[4]);
        }
    }

	
	void Start () {
		// Create Resources folder
		createFolders ();

		// Get Filename
		filename = Path.GetFileName(dataPath);

		loadScene ();
	}



	void loadScene(){
		// Check if the PointCloud was loaded previously
		if(!Directory.Exists (Application.dataPath + "/Resources/PointCloudMeshes/" + filename)){
			UnityEditor.AssetDatabase.CreateFolder ("Assets/Resources/PointCloudMeshes", filename);
			loadPointCloud ();
		} else if (forceReload){
			UnityEditor.FileUtil.DeleteFileOrDirectory(Application.dataPath + "/Resources/PointCloudMeshes/" + filename);
			UnityEditor.AssetDatabase.Refresh();
			UnityEditor.AssetDatabase.CreateFolder ("Assets/Resources/PointCloudMeshes", filename);
			loadPointCloud ();
		} else
			// Load stored PointCloud
			loadStoredMeshes();
	}
	
	
	void loadPointCloud(){
		// Check what file exists
		if (File.Exists (Application.dataPath + dataPath + ".off")) 
			// load off
			StartCoroutine ("loadOFF", dataPath + ".off");
        else if (File.Exists(Application.dataPath + dataPath + ".ply"))
            StartCoroutine("loadPLY", dataPath + ".ply");
        else
            UnityEngine.Debug.Log ("File '" + dataPath + "' could not be found"); 
	}
	
	// Load stored PointCloud
	void loadStoredMeshes(){

		UnityEngine.Debug.Log ("Using previously loaded PointCloud: " + filename);

		GameObject pointGroup = Instantiate(Resources.Load ("PointCloudMeshes/" + filename)) as GameObject;

		loaded = true;
	}
	
    IEnumerator loadPLY(string dPath)
    {
        //file stream reader
        StreamReader sr = new StreamReader(Application.dataPath + dPath);
        string buffer = "";
        int n_points = 0;

        //read file header
        while (buffer != "end_header")
        {
            buffer = sr.ReadLine();

            if (buffer.Contains("element vertex"))
                n_points = int.Parse(buffer.Split()[2]); 
        }

        int current_class = -1;
        int current_obj_id = -1;
        int current_point = 0;
        List<Vector3> obj_points = new List<Vector3>();
        pointCloud = new GameObject(filename);

        while ((buffer = sr.ReadLine()) != null)
        {
            current_point++;
            PLYDataLine data = new PLYDataLine(buffer, invertYZ);
            
            if(data.obj_class != current_class || data.obj_id != current_obj_id)
            {
                if(current_point != 1)
                    InstantiatePLYMesh(obj_points.ToArray(), current_class, current_obj_id);

                current_class = data.obj_class;
                current_obj_id = data.obj_id;
                obj_points.Clear();
            }

            obj_points.Add(data.pos);

            progress = current_point * 1.0f / n_points * 1.0f;
            if (current_point % Mathf.FloorToInt(n_points / 20) == 0)
            {
                guiText = current_point.ToString() + " out of " + n_points.ToString() + " points loaded";
                yield return null;
            }
        }

        InstantiatePLYMesh(obj_points.ToArray(), current_class, current_obj_id);

        UnityEditor.PrefabUtility.CreatePrefab("Assets/Resources/PointCloudMeshes/" + filename + ".prefab", pointCloud);
        loaded = true;
    }

    public void InstantiatePLYMesh(Vector3[] points, int current_class, int current_obj_id)
    {
        string obj_identifier = filename + "_" + class_names[current_class] + "_" + current_obj_id;
        GameObject pointGroup = new GameObject(obj_identifier);
        pointGroup.AddComponent<MeshFilter>();
        pointGroup.AddComponent<MeshRenderer>();
        pointGroup.GetComponent<Renderer>().material = matVertex;

        pointGroup.GetComponent<MeshFilter>().mesh = CreatePLYMesh(points, current_class);

        if (current_class == 5)
        {
            AddRebuiltMesh(pointGroup, current_class, current_obj_id);
        }
        pointGroup.transform.parent = pointCloud.transform;

        // Store Mesh
        UnityEditor.AssetDatabase.CreateAsset(pointGroup.GetComponent<MeshFilter>().mesh, "Assets/Resources/PointCloudMeshes/" + filename + @"/" + obj_identifier + ".asset");
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
    }

    public void AddRebuiltMesh(GameObject go, int cl, int id)
    {
        StorePointCloudMesh(go.GetComponent<MeshFilter>().mesh, cl, id);
        RunMeshLabScript(cl, id);
        GameObject rebuilt_mesh = ReadMeshlabOutput(cl, id);

        rebuilt_mesh.transform.parent = go.transform;
    }
    
    public GameObject ReadMeshlabOutput(int cl, int id)
    {
        StreamReader sr = new StreamReader(Application.dataPath + "/Resources/TempPointCloudMeshes/" + filename + "/" + class_names[cl] + "_" + id + "_tri.ply");
        string buffer = "";
        int n_points = 0;
        int n_faces = 0;

        //read file header
        while (buffer != "end_header")
        {
            buffer = sr.ReadLine();

            if (buffer.Contains("element vertex"))
                n_points = int.Parse(buffer.Split()[2]);

            if(buffer.Contains("element face"))
                n_faces = int.Parse(buffer.Split()[2]);
        }

        List<Vector3> obj_points = new List<Vector3>();
        List<Vector3> obj_normals = new List<Vector3>();
        List<int> obj_faces = new List<int>();

        for(int i = 0; i < n_points; i++)
        {
            buffer = sr.ReadLine();
            string[] data_split = buffer.Replace('.', ',').Trim().Split();

            obj_points.Add(new Vector3(float.Parse(data_split[0]), float.Parse(data_split[2]), float.Parse(data_split[1])));
            obj_normals.Add(new Vector3(float.Parse(data_split[3]), float.Parse(data_split[5]), float.Parse(data_split[4])));
        }

        for (int i = 0; i < n_faces; i++)
        {
            buffer = sr.ReadLine();
            string[] data_split = buffer.Replace('.', ',').Trim().Split();

            obj_faces.Add(int.Parse(data_split[1]));
            obj_faces.Add(int.Parse(data_split[2]));
            obj_faces.Add(int.Parse(data_split[3]));
        }


        GameObject go = new GameObject(class_names[cl] + "_" + id + "_mesh");

        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        mesh.vertices = obj_points.ToArray();
        mesh.triangles = obj_faces.ToArray();
        //mesh.normals = obj_normals.ToArray();
        mesh.RecalculateNormals();
        go.GetComponent<MeshFilter>().mesh = mesh;
        go.GetComponent<Renderer>().material = matSurface;//(Material) Resources.Load("Materials/Car Material.mat");//new Material(Shader.Find("Diffuse"));//GameObject.Find("Cube").GetComponent<Renderer>().material;
        Resources.Load("Materials/Car Material.mat");

        GameObject go2 = new GameObject(class_names[cl] + "_" + id + "_mesh_inv");

        go2.AddComponent<MeshFilter>();
        go2.AddComponent<MeshRenderer>();
        Mesh mesh2 = new Mesh();
        mesh2.vertices = obj_points.ToArray();
        //mesh2.triangles = obj_faces.ToArray();

        var indices = mesh.triangles;
        var triangleCount = indices.Length / 3;
        for (var i = 0; i < triangleCount; i++)
        {
            var tmp = indices[i * 3];
            indices[i * 3] = indices[i * 3 + 1];
            indices[i * 3 + 1] = tmp;
        }
        mesh2.triangles = indices;

        //List<Vector3> obj_normals2 = new List<Vector3>();
        //foreach (Vector3 n in obj_normals)
        //    obj_normals2.Add(Vector3.Scale(n, reverse_vector));
        //mesh2.normals = obj_normals2.ToArray();
        mesh2.RecalculateNormals();
        go2.GetComponent<MeshFilter>().mesh = mesh2;
        go2.GetComponent<Renderer>().material = matSurface;//(Material)Resources.Load("Materials/Car Material.mat");//new Material(Shader.Find("Diffuse"));//GameObject.Find("Cube").GetComponent<Renderer>().material;
        go2.transform.parent = go.transform;

        UnityEditor.AssetDatabase.CreateAsset(go.GetComponent<MeshFilter>().mesh, "Assets/Resources/PointCloudMeshes/" + filename + @"/" + class_names[cl] + "_" + id + "_mesh" + ".asset");
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        UnityEditor.AssetDatabase.CreateAsset(go2.GetComponent<MeshFilter>().mesh, "Assets/Resources/PointCloudMeshes/" + filename + @"/" + class_names[cl] + "_" + id + "_mesh_inv" + ".asset");
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        return go;
    }

    public void RunMeshLabScript(int cl, int id)
    {
        string input_path = Application.dataPath + "/Resources/TempPointCloudMeshes/" + filename + "/" + class_names[cl] + "_" + id + ".asc";
        string output_path = Application.dataPath + "/Resources/TempPointCloudMeshes/" + filename + "/" + class_names[cl] + "_" + id + "_tri.ply";
        string script_path = Application.dataPath + "/Meshlab Scripts/script2.mlx";

        UnityEngine.Debug.Log(input_path);

        input_path.Replace('/', '\\');
        output_path.Replace('/', '\\');
        script_path.Replace('/', '\\');

        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = "meshlabserver";
        info.Arguments = String.Format(" -i \"{0}\" -o \"{1}\" -m vn sa -s \"{2}\"", input_path, output_path, script_path);
        info.CreateNoWindow = true;
        info.UseShellExecute = true;

        Process process = Process.Start(info);
        process.WaitForExit();
        process.Close();
    }

    public void StorePointCloudMesh(Mesh mesh, int cl, int id)
    {
        if(!Directory.Exists(Application.dataPath + "/Resources/TempPointCloudMeshes/" + filename))
            UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/TempPointCloudMeshes", filename);

        using (StreamWriter sw = new StreamWriter(Application.dataPath + "/Resources/TempPointCloudMeshes/" + filename + "/" + class_names[cl] + "_" + id + ".asc"))
            foreach (Vector3 vert in mesh.vertices)
            {
                string line = vert.x + " " + vert.z + " " + vert.y;
                sw.WriteLine(line.Replace(',', '.'));
            }
    }

    public Mesh CreatePLYMesh(Vector3[] points, int current_class)
    {
        Mesh mesh = new Mesh();

        int[] indecies = new int[points.Length];
        Color[] myColors = new Color[points.Length];

        for (int i = 0; i < points.Length; ++i)
        {
            indecies[i] = i;
            myColors[i] = class_colors[current_class];
        }


        mesh.vertices = points;
        mesh.colors = myColors;
        mesh.SetIndices(indecies, MeshTopology.Points, 0);

        return mesh;
    }

    // Start Coroutine of reading the points from the OFF file and creating the meshes
    IEnumerator loadOFF(string dPath){

		// Read file
		StreamReader sr = new StreamReader (Application.dataPath + dPath);
		sr.ReadLine (); // OFF
		string[] buffer = sr.ReadLine ().Split(); // nPoints, nFaces
		
		numPoints = int.Parse (buffer[0]);
		points = new Vector3[numPoints];
		colors = new Color[numPoints];
		minValue = new Vector3();
		
		for (int i = 0; i< numPoints; i++){
			buffer = sr.ReadLine ().Split ();
            buffer = cleanBuffer(buffer);
			if (!invertYZ)
				points[i] = new Vector3 (float.Parse (buffer[0])*scale, float.Parse (buffer[1])*scale,float.Parse (buffer[2])*scale) ;
			else
				points[i] = new Vector3 (float.Parse (buffer[0])*scale, float.Parse (buffer[2])*scale,float.Parse (buffer[1])*scale) ;
			
			if (buffer.Length >= 5)
				colors[i] = new Color (int.Parse (buffer[3])/255.0f,int.Parse (buffer[4])/255.0f,int.Parse (buffer[5])/255.0f);
			else
				colors[i] = Color.cyan;

			// Relocate Points near the origin
			//calculateMin(points[i]);

			// GUI
			progress = i *1.0f/(numPoints-1)*1.0f;
			if (i%Mathf.FloorToInt(numPoints/20) == 0){
				guiText=i.ToString() + " out of " + numPoints.ToString() + " loaded";
				yield return null;
			}
		}

		
		// Instantiate Point Groups
		numPointGroups = Mathf.CeilToInt (numPoints*1.0f / limitPoints*1.0f);

		pointCloud = new GameObject (filename);

		for (int i = 0; i < numPointGroups-1; i ++) {
			InstantiateMesh (i, limitPoints);
			if (i%10==0){
				guiText = i.ToString() + " out of " + numPointGroups.ToString() + " PointGroups loaded";
				yield return null;
			}
		}
		InstantiateMesh (numPointGroups-1, numPoints- (numPointGroups-1) * limitPoints);

		//Store PointCloud
		UnityEditor.PrefabUtility.CreatePrefab ("Assets/Resources/PointCloudMeshes/" + filename + ".prefab", pointCloud);

		loaded = true;
	}

    private string[] cleanBuffer(string[] buffer)
    {
        ArrayList new_buffer = new ArrayList();
        foreach (string s in buffer)
        {
            string s2 = s.Replace('.', ',');
            if (s2 != "")
                new_buffer.Add(s2);
        }
        return (String[]) new_buffer.ToArray(typeof(string));
    }

    void InstantiateMesh(int meshInd, int nPoints){
		// Create Mesh
		GameObject pointGroup = new GameObject (filename + meshInd);
		pointGroup.AddComponent<MeshFilter> ();
		pointGroup.AddComponent<MeshRenderer> ();
		pointGroup.GetComponent<Renderer>().material = matVertex;

		pointGroup.GetComponent<MeshFilter> ().mesh = CreateMesh (meshInd, nPoints, limitPoints);
		pointGroup.transform.parent = pointCloud.transform;


		// Store Mesh
		UnityEditor.AssetDatabase.CreateAsset(pointGroup.GetComponent<MeshFilter> ().mesh, "Assets/Resources/PointCloudMeshes/" + filename + @"/" + filename + meshInd + ".asset");
		UnityEditor.AssetDatabase.SaveAssets ();
		UnityEditor.AssetDatabase.Refresh();
	}

	Mesh CreateMesh(int id, int nPoints, int limitPoints){
		
		Mesh mesh = new Mesh ();
		
		Vector3[] myPoints = new Vector3[nPoints]; 
		int[] indecies = new int[nPoints];
		Color[] myColors = new Color[nPoints];

		for(int i=0;i<nPoints;++i) {
			myPoints[i] = points[id*limitPoints + i] - minValue;
			indecies[i] = i;
			myColors[i] = colors[id*limitPoints + i];
		}

        
		mesh.vertices = myPoints;
		mesh.colors = myColors;
		mesh.SetIndices(indecies, MeshTopology.Points,0);
		//mesh.uv = new Vector2[nPoints];
		//mesh.normals = new Vector3[nPoints];


		return mesh;
	}

	void calculateMin(Vector3 point){
		if (minValue.magnitude == 0)
			minValue = point;


		if (point.x < minValue.x)
			minValue.x = point.x;
		if (point.y < minValue.y)
			minValue.y = point.y;
		if (point.z < minValue.z)
			minValue.z = point.z;
	}

	void createFolders(){
		if(!Directory.Exists (Application.dataPath + "/Resources/"))
			UnityEditor.AssetDatabase.CreateFolder ("Assets", "Resources");

		if (!Directory.Exists (Application.dataPath + "/Resources/PointCloudMeshes/"))
			UnityEditor.AssetDatabase.CreateFolder ("Assets/Resources", "PointCloudMeshes");
	}


	void OnGUI(){


		if (!loaded){
			GUI.BeginGroup (new Rect(Screen.width/2-100, Screen.height/2, 400.0f, 20));
			GUI.Box (new Rect (0, 0, 300.0f, 20.0f), guiText);
			GUI.Box (new Rect (0, 0, progress*300.0f, 20), "");
			GUI.EndGroup ();
		}
	}

}
