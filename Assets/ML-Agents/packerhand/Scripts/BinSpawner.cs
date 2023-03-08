using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;


namespace Bins {
public class Container
{
    public float Length {get; set;}
    public float Width {get; set;}
    public float Height {get; set;}
}



public class BinSpawner : MonoBehaviour {


public static List<Container> Containers = new List<Container>();
public Container Container = new Container();
public GameObject binArea; // The bin container prefab, which will be manually selected in the Inspector
public GameObject outerBin; // The outer shell of container prefab, which will be manually selected in the Inspector
public GameObject Origin; // gives origin position of the first bin (for multiplatform usage)   
[HideInInspector] public List<Vector4> origins = new List<Vector4>(); 

[HideInInspector] public List<CombineMesh> m_BackMeshScripts = new List<CombineMesh>();
[HideInInspector] public List<CombineMesh> m_SideMeshScripts = new List<CombineMesh>();
[HideInInspector] public List<CombineMesh> m_BottomMeshScripts = new List<CombineMesh>();

public List<float> binscales_x; 
public List<float> binscales_y;
public List<float> binscales_z;
// prefab's (BinIso20) sizes
public float biniso_z = 59f;
public float biniso_x = 23.5f;
public float biniso_y = 23.9f;
public int total_bin_num;
public float total_bin_volume;
string homeDir;

    public void Start()
    {
        homeDir = Environment.GetEnvironmentVariable("HOME");
    }

    public void SetUpBins(string box_type, int seed=123)
    {
        if (box_type == "random_multibin")
        {
            // read bin size from file
            RandomBinGenerator();
        }
        else
        {
            // read bin size from file
            ReadJson(box_type);            
        }
        Vector3 localOrigin = Origin.transform.position;
        int idx = 0;
        foreach (Container c in Containers)
        {
            // make container and outer_shell from prefab
            GameObject container = Instantiate(binArea);
            GameObject shell = Instantiate(outerBin);
            container.name = $"Bin{idx.ToString()}";
            shell.name = $"OuterBin{idx.ToString()}";
            float binscale_x = c.Width;
            float binscale_y  = c.Height;
            float binscale_z  = c.Length;
            binscales_x.Add(binscale_x);
            binscales_y.Add(binscale_y);
            binscales_z.Add(binscale_z);
            // Set bin and outer bin's scale and position
            container.transform.localScale = new Vector3((binscale_x/biniso_x), (binscale_y/biniso_y), (binscale_z/biniso_z));
            //Debug.Log($"CONTAINER LOCALSCALE IS: {container.transform.localScale}");
            shell.transform.localScale = new Vector3(binscale_x/biniso_x, binscale_y/biniso_y, binscale_z/biniso_z);
            // Set origin position of each bin
            localOrigin.x = localOrigin.x+binscale_x+5f;
            Vector4 originInfo = new Vector4(localOrigin.x, localOrigin.y, localOrigin.z, idx);
            Debug.Log($"ORIGIN INFO FOR BIN {idx}: {originInfo}");
            origins.Add(originInfo);
            Vector3 container_center = new Vector3(localOrigin.x+(binscale_x/2f), 0.5f, localOrigin.z+(binscale_z/2f));
            container.transform.localPosition = container_center;
            shell.transform.localPosition = container_center;
            // Cache bin's scripts and initialize their agent
            CombineMesh binBottomScript = container.transform.GetChild(0).GetComponent<CombineMesh>();
            CombineMesh binBackScript = container.transform.GetChild(1).GetComponent<CombineMesh>();
            CombineMesh binSideScript = container.transform.GetChild(2).GetComponent<CombineMesh>();
            m_BottomMeshScripts.Add(binBottomScript);
            m_SideMeshScripts.Add(binSideScript);
            m_BackMeshScripts.Add(binBackScript);
            // update total volume
            total_bin_volume += binscale_x * binscale_y * binscale_z;
            idx++;
        }
        total_bin_num = idx;
        // hide original prefabs
        binArea.SetActive(false);
        outerBin.SetActive(false);

    }
    public void RandomBinGenerator()
    {
        //Container.Width = (float) Math.Round(UnityEngine.Random.Range(10.0f, 30.0f));
    }



    public void ReadJson(string box_file) 
    {
        homeDir = Environment.GetEnvironmentVariable("HOME");
        string filename = $"{homeDir}/Unity/data/{box_file}.json";
        using (var inputStream = File.Open(filename, FileMode.Open)) {
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(inputStream, new System.Xml.XmlDictionaryReaderQuotas()); 
            //var root = XElement.Load(jsonReader);
            var root = XDocument.Load(jsonReader);
            var containers = root.XPathSelectElement("//Container").Elements();
            foreach (XElement container in containers)
            {
                float length = float.Parse(container.XPathSelectElement("./Length").Value)/10f;
                float width = float.Parse(container.XPathSelectElement("./Width").Value)/10f;
                float height = float.Parse(container.XPathSelectElement("./Height").Value)/10f;   
                //Debug.Log($"JSON CONTAINER LENGTH {Container.Length} WIDTH {Container.Width} HEIGHT {Container.Height}");
                Containers.Add(new Container
                    {
                        Length = length,
                        Width = width,
                        Height = height,
                    });
            }
        }
    }

}
}