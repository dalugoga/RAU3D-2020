using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudSegmentation : MonoBehaviour
{
    public class PointCloudSegmentClass
    {
        public Color class_color;
        public string class_name;

        public PointCloudSegmentClass(string name, Color color)
        {
            this.class_name = name;
            this.class_color = color;
        }
    }

    public class PointCloudSegment
    {
        public PointCloudSegmentClass segment_class;
        public List<int> segment_points;

        public PointCloudSegment(PointCloudSegmentClass clss, List<int> points)
        {
            this.segment_points = points;
            this.segment_class = clss;
        }
    }

    List<PointCloudSegment> segments;
    List<PointCloudSegmentClass> classes;

    // Start is called before the first frame update
    void Start()
    {
        segments = new List<PointCloudSegment>();
        classes = new List<PointCloudSegmentClass>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddClassAndSegmentation(List<int> points, PointCloudSegmentClass clss)
    {
        classes.Add(clss);
        segments.Add(new PointCloudSegment(clss, points));
        GameObject.Find("Cloud Selecter").GetComponent<CloudSelector>().Segmentation(points, clss.class_color);
        GameObject.Find("Segmentation Canvas").SetActive(false);
    }

    public void AddSegmentation(List<int> points, string clss_name)
    {
        PointCloudSegmentClass clss = null;

        foreach (PointCloudSegmentClass c in classes)
            if (c.class_name == clss_name)
            {
                clss = c;
                break;
            }

        if(clss != null)
        {
            segments.Add(new PointCloudSegment(clss, points));
            GameObject.Find("Cloud Selecter").GetComponent<CloudSelector>().Segmentation(points, clss.class_color);
            GameObject.Find("Segmentation Canvas").SetActive(false);
        }
    }

    public List<PointCloudSegment> GetSegments()
    {
        return segments;
    }

    public List<PointCloudSegmentClass> GetClasses()
    {
        return classes;
    }
}
