using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static PointCloudSegmentation;
using System.Linq;

public class MyCanvasScripts : MonoBehaviour
{
    public Slider slider_r;
    public Slider slider_g;
    public Slider slider_b;
    public Image panel_color;
    public InputField class_name;
    public CloudSelector cloud_selector;
    public Dropdown classes_dropdown;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        panel_color.color = new Color(slider_r.value, slider_g.value, slider_b.value);
    }

    public void CreateNewClassOnClick()
    {
        if (class_name.text.Length == 0)
            return;

        GameObject cloud = cloud_selector.GetSelectedCloud();
        List<int> selected_points = cloud_selector.GetSelectedVertices().ToList();
        PointCloudSegmentation cloud_segmentation = cloud.GetComponent<PointCloudSegmentation>();

        PointCloudSegmentClass clss = new PointCloudSegmentClass(class_name.text, panel_color.color);

        cloud_segmentation.AddClassAndSegmentation(selected_points, clss);

        classes_dropdown.AddOptions(new List<string> { class_name.text });
        class_name.text = "";
    }

    public void SaveNewObjectOnClick()
    {
        GameObject cloud = cloud_selector.GetSelectedCloud(); 
        PointCloudSegmentation cloud_segmentation = cloud.GetComponent<PointCloudSegmentation>();
        List<int> selected_points = cloud_selector.GetSelectedVertices().ToList();

        string selected_class_name = classes_dropdown.options[classes_dropdown.value].text;

        cloud_segmentation.AddSegmentation(selected_points, selected_class_name);

    }

    public void KeyboardKeyOnClick(GameObject button)
    {
        class_name.text = class_name.text + button.transform.GetChild(0).gameObject.GetComponent<Text>().text;
    }

    public void KeyboardBackspaceOnClick()
    {
        if(class_name.text.Length != 0)
            class_name.text = class_name.text.Remove(class_name.text.Length - 1);
    }

    public void KeyboardSpaceOnClick()
    {
        class_name.text = class_name.text + " ";
    }

    public void KeyboardShiftOnClick()
    {
        foreach(GameObject button in GameObject.FindGameObjectsWithTag("ABCKey"))
        {
            char c = button.transform.GetChild(0).gameObject.GetComponent<Text>().text[0];
            if (char.IsUpper(c))
                c = char.ToLower(c);
            else
                c = char.ToUpper(c);

            button.transform.GetChild(0).gameObject.GetComponent<Text>().text = c.ToString();
        }

    }


}
