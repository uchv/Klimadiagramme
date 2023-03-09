using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Klimadiagramm : MonoBehaviour
{
    private float[] temperatures = new float[12];
    private float[] precipitation = new float[12];
    private string locationName = "";
    private int locationHeight = 0;
    
    private int numLabelsPerAxis = 0;


    [Header("General Settings")]
    public Vector2 graphSpaceSize = new Vector2(400.0f, 600.0f);

    [Header("Colors")]
    public Color precipitationAreaColor;
    public Color extremePrecipitationAreaColor;
    public Color temperatureAreaColor;
    public Color precipitationLineColor;
    public Color temperatureLineColor;
    public Material lineMaterial;

    [Header("GameObject pointers")]
    public GameObject infoLocation;
    public GameObject infoTemperatureAverage;
    public GameObject infoPrecipitationTotal;

    public GameObject temperaturePointPrefab;
    public GameObject precipitationPointPrefab;


    [Header("Debugging")]
    public bool drawFull = true;
    public bool drawPartial = true;



    void Start()
    {
        // make sure there is already something there for startup
        temperatures = new float[12] { 
           8.0f, 9.0f, 10.0f, 13.0f, 18.0f, 21.0f, 23.0f, 22.0f, 20.0f, 15.0f, 10.5f, 7.0f
        };

        precipitation = new float[12] {
            80.0f, 90.0f, 70.0f, 66.0f, 58.0f, 36.0f, 17.0f, 24.0f, 60.0f, 120.0f, 110.0f, 96.0f
        };

        RebuildGraphMeshes();
    }


    // retrieves data from input fields and rebuilds
    public void UpdateData()
    {
        KDInput[] inputs = transform.parent.GetComponentsInChildren<KDInput>();

        foreach(KDInput ins in inputs)
        {
            float value = ins.GetValue();
            
            // if input field hasn't been set yet, -999.0f is returned
            if(value == -999.0f)
            {
                continue;
            }

            if(ins.typ == KDInput.Datentyp.TEMPERATUR)
            {
                temperatures[(int)ins.monthIndex] = value;
            }
            else
            {
                precipitation[(int)ins.monthIndex] = value;
            }
        }

        RebuildGraphMeshes();
    }


    // sets info label 
    public void SetLocation(string location)
    {
        infoLocation.GetComponent<TextMesh>().text = $"{location} ({locationHeight} m)";
        locationName = location;      
    }

    // sets info label
    public void SetLocationHeight(string height)
    {
        infoLocation.GetComponent<TextMesh>().text = $"{locationName} ({height} m)";
        locationHeight = int.Parse(height);
    }


    // rebuilds axes, labels, etc. 
    private void RebuildFrame()
    {
        float highestTemp = 0.0f, lowestTemp = 0.0f;
        float highestPrec = 0.0f, lowestPrec = 0.0f;

        for(int i = 0; i < 12; i++)
        {
            float temp = temperatures[i];
            float prec = precipitation[i] * 0.5f;

            highestTemp = temp > highestTemp ? temp : highestTemp;
            lowestTemp = temp < lowestTemp ? temp : lowestTemp;
            highestPrec = prec > highestPrec ? prec : highestPrec;
            lowestPrec = prec < lowestPrec ? prec : lowestPrec;
        }

        Debug.Log($"highest temperature: {highestTemp}, lowest temperature: {lowestTemp}");
        Debug.Log($"highest precipitation: {highestPrec}, lowest precipitation: {lowestPrec}");

        float highestValue = Mathf.Max(highestTemp, highestPrec);
        float lowestValue = Mathf.Max(lowestTemp, lowestPrec);

        // 6 is limit as everything gets compressed above 100
        int numSteps = Mathf.Min(6, Mathf.CeilToInt(highestValue / 10.0f)) + Mathf.FloorToInt(lowestValue/ 10.0f) + 1;
        int lowestStep = Mathf.FloorToInt(lowestValue / 10.0f);


        // find all previous labels and destroy them before creating new ones
        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("axisLabel"))
        {
            Destroy(obj);
        }

        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("valueAxis"))
        {
            Destroy(obj);
        }


        for(int i = lowestStep; i < lowestStep + numSteps; i++)
        {
            // create label objects
            GameObject tempLabel = new GameObject("templabel");
            tempLabel.tag = "axisLabel";

            GameObject precLabel = new GameObject("precLabel");
            precLabel.tag = "axisLabel";

            tempLabel.AddComponent<TextMesh>().text = (i * 10).ToString();
            precLabel.AddComponent<TextMesh>().text = (i * 20).ToString();
            
            // now position them
            tempLabel.transform.SetParent(transform);
            precLabel.transform.SetParent(transform);

            tempLabel.transform.localPosition = new Vector2(0.0f, graphSpaceSize.y / numSteps * i);
            precLabel.transform.localPosition = new Vector2(graphSpaceSize.x, graphSpaceSize.y / numSteps * i);
            tempLabel.transform.localScale = new Vector3(13.0f, 13.0f, 1.0f);
            precLabel.transform.localScale = new Vector3(13.0f, 13.0f, 1.0f);
            tempLabel.GetComponent<TextMesh>().anchor = TextAnchor.MiddleRight;
            precLabel.GetComponent<TextMesh>().anchor = TextAnchor.MiddleLeft;
            tempLabel.GetComponent<TextMesh>().color = temperatureLineColor;
            precLabel.GetComponent<TextMesh>().color = precipitationLineColor;


            // value axis
            GameObject axis = new GameObject("valueAxis");
            axis.tag = "valueAxis";
            axis.transform.SetParent(transform);
            // different z-value to let meshes cover lines
            axis.transform.localPosition = new Vector3(0.0f, 0.0f, 1.0f);
            LineRenderer lr = axis.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.startWidth = i == 0 ? 1.5f : 0.7f;
            lr.endWidth = i == 0 ? 1.5f : 0.7f;
            lr.material = lineMaterial;
            lr.startColor = new Color(0.25f, 0.25f, 0.25f);
            lr.endColor = new Color(0.25f, 0.25f, 0.25f);

            const float offset = 5.0f;
            lr.SetPosition(0, new Vector3(offset, graphSpaceSize.y / numSteps * i, 0.0f));
            lr.SetPosition(1, new Vector3(graphSpaceSize.x - offset, graphSpaceSize.y / numSteps * i, 0.0f));
        }
        
        numLabelsPerAxis = numSteps;
        
        
        UpdateInfoLabels();
    }


    // rebuilds meshes representing humid, very humid, dry areas. also draws temperature/precipitation lines and points + month axes.
    public void RebuildGraphMeshes()
    {
        // make sure graph frame fits data
        RebuildFrame();


        // delete previous data point sprites
        foreach(GameObject obj in GameObject.FindGameObjectsWithTag("dataPoint"))
        {
            Destroy(obj);
        }
        
        
        // draw month axes. gameobjects have already been placed in the editor.
        int month = 0;
        foreach(GameObject go in GameObject.FindGameObjectsWithTag("monthAxis"))
        {
            LineRenderer line = go.GetComponent<LineRenderer>();
            if(line)
            {
                // multiplying by two as temperature axis is less dense than precipitation. halfing precipitation doesn't work 
                // as it messes with compression above 100mm
                line.SetPosition(0, DataToMeshVertex(month, Mathf.Min(0.0f, temperatures[month] * 2.0f), false));
                line.SetPosition(1, DataToMeshVertex(month, Mathf.Max(temperatures[month] * 2.0f, precipitation[month]), false));
                
                month += 1;
            }
        }

        // create lines
        LineRenderer[] lr = GetComponentsInChildren<LineRenderer>(false);
        if(lr.Length < 2)
        {
            Debug.Log("Missing temperature/precipitation line components");
            return;
        }

        SetupDataLine(ref lr[0], true);
        SetupDataLine(ref lr[1], false);


        int insertedPointsCount = 0;
        for(int i = 0; i < 12; i++)
        {
            Vector2 tempPos = DataToMeshVertex(i, temperatures[i], true);
            Vector2 precPos = DataToMeshVertex(i, precipitation[i], false);
            
            // add circles at data points
            var tempPointInstance = Instantiate(temperaturePointPrefab);
            tempPointInstance.tag = "dataPoint";
            tempPointInstance.transform.SetParent(transform);
            tempPointInstance.transform.localPosition = new Vector3(tempPos.x, tempPos.y, 0.0f);
            
            var precPointInstance = Instantiate(precipitationPointPrefab);
            precPointInstance.tag = "dataPoint";
            precPointInstance.transform.SetParent(transform);
            precPointInstance.transform.localPosition = new Vector3(precPos.x, precPos.y, 0.0f);
            
            
            // draw lines
            lr[0].SetPosition(i, tempPos);

            // it's a little more complicated for precipitation as values above 100mm get compressed
            lr[1].SetPosition(i + insertedPointsCount, precPos);
            if(i < 11 && ((precipitation[i] < 100.0f && precipitation[i+1] > 100.0f) || 
                (precipitation[i] > 100.0f && precipitation[i+1] < 100.0f)))
            {
                float x = CalcCompressionBreakpoint(precipitation[i], precipitation[i+1]);
                Vector2 pos = DataToMeshVertex((float)i + x, 100.0f, false);

                lr[1].positionCount += 1;
                insertedPointsCount += 1;

                lr[1].SetPosition(i + insertedPointsCount, pos);
            } 
       }


        // we are creating different meshes for each color. this way, each area can be highlighted
        // seperately and shaded differently
        MeshFilter[] mf = gameObject.GetComponentsInChildren<MeshFilter>(false);
        if(mf.Length < 3)
        {
            Debug.Log("Missing mesh components");
            return;
        }

        // humid area - when there is more precipitation than temperature. capped at 100 prec
        Mesh humidMesh = new Mesh();
        humidMesh.name = "humid";
        mf[0].mesh = humidMesh;
        mf[0].gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", precipitationAreaColor);
        List<Vector3> humidVertices = new List<Vector3>();
        int humidIndex = 0;

        // dry area - when there is less precipitation than temperature. capped at 100 prec
        Mesh dryMesh = new Mesh();
        dryMesh.name = "humid";
        mf[1].mesh = dryMesh;
        mf[1].gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", temperatureAreaColor);
        List<Vector3> dryVertices = new List<Vector3>();
        int dryIndex = 0;
           
        // very humid area - compressed; precipitation above 100mm
        Mesh vHumidMesh = new Mesh();
        vHumidMesh.name = "humid";
        mf[2].mesh = vHumidMesh;
        mf[2].gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", extremePrecipitationAreaColor);
        List<Vector3> vHumidVertices = new List<Vector3>();
        int vHumidIndex = 0;


        // now go through every month, drawing only humid and dry areas
        for(int i = 0; i < 11; i++)
        {
            // even temperatures need to be clamped as humid area can't extend below 0
            float curTemp = Mathf.Max(0.0f, temperatures[i]);
            float curPrec = Mathf.Max(precipitation[i], 0.0f);
            float nextTemp = Mathf.Max(0.0f, temperatures[i+1]);
            float nextPrec = Mathf.Max(precipitation[i+1], 0.0f);

            Debug.Log($"Current Temperature: {curTemp}, Current Precipitation: {curPrec}, Next Temperature: {nextTemp}, Next Precipitation: {nextPrec}");

            if(curPrec * 0.5f > curTemp && nextPrec * 0.5f > nextTemp && drawFull)
            {
                int addCount, addHumidCount;
                CalcFullArea(i, curPrec, nextPrec, curTemp, nextTemp, ref humidVertices, ref vHumidVertices, out addCount, out addHumidCount);
                humidIndex += addCount;
                vHumidIndex += addHumidCount;
            }
            else if(curPrec * 0.5f < curTemp && nextPrec * 0.5f < nextTemp && drawFull)
            {
                int addCount, addHumidCount;
                CalcFullArea(i, curPrec, nextPrec, curTemp, nextTemp, ref dryVertices, ref vHumidVertices, out addCount, out addHumidCount);
                dryIndex += addCount;
                vHumidIndex += addHumidCount;
            }
            else if(curPrec * 0.5f > curTemp && nextPrec * 0.5f < nextTemp && drawPartial)
            {
                CalcSeparatedArea(i, curPrec, nextPrec, curTemp, nextTemp, ref humidVertices, ref dryVertices);
                humidIndex += 3;
                dryIndex += 3;
            }
            else if(curPrec * 0.5f < curTemp && nextPrec * 0.5f > nextTemp && drawPartial)
            {
                CalcSeparatedArea(i, curPrec, nextPrec, curTemp, nextTemp, ref dryVertices, ref humidVertices);
                humidIndex += 3;
                dryIndex += 3;
            }            
        }


        int[] humidTriIndices = Enumerable.Range(0, humidIndex).ToArray();
        humidMesh.SetVertices(humidVertices);
        humidMesh.triangles = humidTriIndices;

        int[] dryTriIndices = Enumerable.Range(0, dryIndex).ToArray();
        dryMesh.SetVertices(dryVertices);
        dryMesh.triangles = dryTriIndices;

        int[] vHumidTriIndices = Enumerable.Range(0, vHumidIndex).ToArray();
        vHumidMesh.SetVertices(vHumidVertices);
        vHumidMesh.triangles = vHumidTriIndices;
    }


    // sets up LineRenderer component for temperature / precipitation lines
    private void SetupDataLine(ref LineRenderer lineRenderer, bool temperature)
    {
        lineRenderer.useWorldSpace = false;

        lineRenderer.startColor = temperature ? temperatureLineColor : precipitationLineColor;
        lineRenderer.endColor = temperature ? temperatureLineColor : precipitationLineColor;

        lineRenderer.material = lineMaterial;

        lineRenderer.positionCount = 12;

        lineRenderer.startWidth = 2.0f;
        lineRenderer.endWidth = 2.0f;
    }


    // updates labels displaying average temperature and total downpour
    private void UpdateInfoLabels()
    {
        float sumTemperature = 0.0f;
        float sumPrecipitation = 0.0f;

        for(int i = 0; i < 12; i++)
        {
            sumTemperature +=  temperatures[i];
            sumPrecipitation += precipitation[i];
        }

        // infos
        infoLocation.transform.localPosition = new Vector3(-5.0f, graphSpaceSize.y + 30.0f, 0.0f);

        float temperatureAverage = sumTemperature / 12.0f;
        infoTemperatureAverage.transform.localPosition = new Vector3(-5.0f, graphSpaceSize.y, 0.0f);
        infoTemperatureAverage.GetComponent<TextMesh>().text = temperatureAverage.ToString() + "°C";

        infoPrecipitationTotal.transform.localPosition = new Vector3(graphSpaceSize.x - 10.0f, graphSpaceSize.y, 0.0f);
        infoPrecipitationTotal.GetComponent<TextMesh>().text = sumPrecipitation.ToString() + " mm";     
    }
    

    // converts month & data to unity coordinate
    private Vector3 DataToMeshVertex(float monthIndex, float value, bool temperature)
    {
        Vector3 coord = new Vector3();

        coord.x = (graphSpaceSize.x / 12.0f) * (monthIndex + 0.5f);

        float compressedUnits = Mathf.Max(0.0f, value - 100.0f);
        float spacePerUnit = (graphSpaceSize.y / ((float)numLabelsPerAxis * 10.0f));

        float baseResult = spacePerUnit * Mathf.Min(value, 100.0f);
        float compressedResult = spacePerUnit * compressedUnits * 0.2f;

        coord.y = temperature ? baseResult : (baseResult + compressedResult) * 0.5f;

        return coord;
    }

    private void CalcFullArea(int monthIdx, float curPrec, float nextPrec, float curTemp, float nextTemp, ref List<Vector3> meshVertices, ref List<Vector3> secondaryMeshVertices, out int addedVerticesCount, out int addedHumidVerticesCount)
    {
        Debug.Log("drawing full area");
        
        addedVerticesCount = 0;
        addedHumidVerticesCount = 0;
        
        if((curPrec < 100.0f && nextPrec < 100.0f) || (curPrec > 100.0f && nextPrec > 100.0f))
        {
            meshVertices.Add(DataToMeshVertex(monthIdx, Mathf.Min(100.0f, curPrec), false));
            meshVertices.Add(DataToMeshVertex(monthIdx, curTemp, true));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, Mathf.Min(100.0f, nextPrec), false));

            meshVertices.Add(DataToMeshVertex(monthIdx, curTemp, true));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, nextTemp, true));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, Mathf.Min(100.0f, nextPrec), false));

            addedVerticesCount += 6;
        }
        
        if(curPrec > 100.0f && nextPrec > 100.0f)
        {
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx, curPrec, false));
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx, 100.0f, false));
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx+1, nextPrec, false));

            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx, 100.0f, false));
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx+1, 100.0f, false));
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx+1, nextPrec, false));

            addedHumidVerticesCount += 6;
        }
        else if(curPrec < 100.0f && nextPrec > 100.0f)
        {
            // we need to find the x coord of the point where the normal area goes into compressed area (it's a corner, not a stright line)
            float mysteryPointX = (float)monthIdx + CalcCompressionBreakpoint(curPrec, nextPrec);

            // for this the regular humid area, (n = 5), we'll need 3 tris
            meshVertices.Add(DataToMeshVertex(monthIdx, curTemp, true));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, 100.0f, false));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, nextTemp, true));

            meshVertices.Add(DataToMeshVertex(monthIdx+1, 100.0f, false));
            meshVertices.Add(DataToMeshVertex(monthIdx, curPrec, false));
            meshVertices.Add(DataToMeshVertex(monthIdx, curTemp, true));

            meshVertices.Add(DataToMeshVertex(mysteryPointX, 100.0f, false));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, 100.0f, false));
            meshVertices.Add(DataToMeshVertex(monthIdx, curPrec, false));

            addedVerticesCount += 9;

            // we only need a single triangle for the very humid area
            secondaryMeshVertices.Add(DataToMeshVertex(mysteryPointX, 100.0f, false));
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx+1, 100.0f, false));
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx+1, nextPrec, false));

            addedHumidVerticesCount += 3;
        }
        else if(curPrec > 100.0f && nextPrec < 100.0f)
        {
            // we need to find the x coord of the point where the normal area goes into compressed area (it's a corner, not a stright line)
            float mysteryPointX = (float)monthIdx + CalcCompressionBreakpoint(curPrec, nextPrec);
            
            // for this the regular humid area, (n = 5), we'll need 3 tris
            meshVertices.Add(DataToMeshVertex(monthIdx, curTemp, true));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, nextPrec, false));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, nextTemp, true));

            meshVertices.Add(DataToMeshVertex(monthIdx, 100.0f, false));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, nextPrec, false));
            meshVertices.Add(DataToMeshVertex(monthIdx, curTemp, true));

            meshVertices.Add(DataToMeshVertex(mysteryPointX, 100.0f, false));
            meshVertices.Add(DataToMeshVertex(monthIdx, 100.0f, false));
            meshVertices.Add(DataToMeshVertex(monthIdx+1, nextPrec, false));

            addedVerticesCount += 9;

            // we only need a single triangle for the very humid area
            secondaryMeshVertices.Add(DataToMeshVertex(mysteryPointX, 100.0f, false));
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx, 100.0f, false));
            secondaryMeshVertices.Add(DataToMeshVertex(monthIdx, curPrec, false));

            addedHumidVerticesCount += 3;
        }
    }


    private void CalcSeparatedArea(int monthIdx, float curPrec, float nextPrec, float curTemp, float nextTemp, ref List<Vector3> firstVertices, ref List<Vector3> secondVertices)
    {
        Debug.Log("drawing separated area");

        Vector2 intersection = CalcIntersection(new Vector2(monthIdx, curPrec * 0.5f), (new Vector2(monthIdx + 1, nextPrec * 0.5f) - new Vector2(monthIdx, curPrec * 0.5f)),
            new Vector2(monthIdx, curTemp), (new Vector2(monthIdx + 1, nextTemp) - new Vector2(monthIdx, curTemp)));
        Vector3 midPoint = DataToMeshVertex(intersection.x, intersection.y, true);

        // draw humid triangle
        firstVertices.Add(DataToMeshVertex(monthIdx, curPrec, false));
        firstVertices.Add(DataToMeshVertex(monthIdx, curTemp, true));
        firstVertices.Add(midPoint);

        // draw dry triangle
        secondVertices.Add(DataToMeshVertex(monthIdx + 1, nextPrec, false));
        secondVertices.Add(DataToMeshVertex(monthIdx + 1, nextTemp, true));
        secondVertices.Add(midPoint);
    }


    private Vector2 CalcIntersection(Vector2 line1Start, Vector2 line1Vec, Vector2 line2Start, Vector2 line2Vec)
    {
        float t;
        t = (line2Start.y - line1Start.y) / (line1Vec.y - line2Vec.y);

        return line1Start + line1Vec * t;
    }


    private float CalcCompressionBreakpoint(float value1, float value2)
    {
        float sectionLength;
        if(value1 < value2)
        {
            sectionLength = (100.0f - value1) / (value2 - value1);
        }
        else 
        {
            sectionLength = 1.0f - (100.0f - value2) / (value1 - value2);
        }

        return sectionLength;
    }
}
