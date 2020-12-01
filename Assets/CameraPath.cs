﻿// using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
// using System.Text.Json;
// using System.Text.Json.Serialization;
public class CameraPath : MonoBehaviour
{
    private Camera myCamera;
    SpherePath myPath;
    string screenshotName = "pic";
    int currentScreenshotID = 0;
    public Transform focusObject;
    bool playdatshit = false;
    public GameObject realObjects;
    public GameObject segmentObjects;
    private Dictionary<string, int> objectType;
    void Awake()
    {
        myCamera = gameObject.GetComponent<Camera>();
        myPath = new SpherePath(4, 5, 3f, -0.1f, Mathf.PI/2);
        myPath.generatePath();
        objectType = new Dictionary<string, int>();
        // TEMPORARY
        // objectType["background"] = 0;
        objectType["spY"] = 1;
        objectType["spB"] = 2;
        // END TEMP
    }
    void Update()
    {
        if(Input.GetMouseButtonDown(0) && !playdatshit){
            playdatshit = true;
            // realObjects.SetActive(true);
            // segmentObjects.SetActive(false);
        }
        // if(Input.GetMouseButtonDown(0)){
        //     RaycastHit hit;
        //     Ray ray;
        //     ray = myCamera.ScreenPointToRay(Input.mousePosition);
        //     if (Physics.Raycast(ray, out hit)) {
        //         Debug.Log("Tag: "+hit.transform.tag+"  position: "+Input.mousePosition);
        //     }
        // }
        // Debug.Log("Iterating path");
        if(playdatshit){
            if(myPath.next()){
                transform.position = focusObject.position + myPath.nextPosition();
                // Debug.Log("focusObject.position: "+focusObject.position+"  myPath.currentPosition: "+myPath.positions[myPath.currentID-1]);
                transform.LookAt(focusObject.position);
                // Debug.Log("transform.position: "+transform.position+"  combined: "+(myPath.positions[myPath.currentID-1]+focusObject.position));
                string scrPath;
                scrPath = Application.persistentDataPath + "/" + "pic" + myPath.currentID.ToString() + ".png";
                ScreenCapture.CaptureScreenshot(scrPath, 1);
                scrPath = Application.persistentDataPath + "/" + "seg" + myPath.currentID.ToString() + ".png";
                // RaycastHit hit;
                // Ray ray;
                // Vector3 screenUV = new Vector3(Screen.height-1,0f,0f);
                // Texture2D segmentationImage = new Texture2D(Screen.width, Screen.height, TextureFormat.Alpha8, false);
                // ray = myCamera.ScreenPointToRay(screenUV);
                // if (Physics.Raycast(ray, out hit)) {
                //     if(objectType.ContainsKey(hit.transform.tag)){
                //         Debug.Log("Known tag: "+hit.transform.tag);
                //         segmentationImage.SetPixel(0, Screen.height-1, new Color(0f, 0f, 0f, objectType[hit.transform.tag]/255f));
                //     } else {
                //         Debug.Log("[ERROR]: Unknown tag: "+hit.transform.tag);
                //     }
                // }
                List<int> lengths = new List<int>(); // starting from with negative as 0
                List<Vector3[]> objectRectangles = probeImage(9, 6); // assuming simple landscape // ok
                Dictionary<int,Dictionary<int, Vector2Int>> lineAnnotation = rectangles2Lines1Tag(objectRectangles);
                List<int> codedMask = getRLEFromLines(Screen.width, Screen.height, lineAnnotation);
                using(TextWriter tw = new StreamWriter(Application.persistentDataPath + "/" + "seg" + myPath.currentID.ToString()+".txt"))
                {
                    // string jsonString = JsonUtility.ToJson(codedMask, true);
                    // string jsonString2 = JsonUtility.ToJson(new int[]{Screen.width, Screen.height});
                    // tw.Write(jsonString2);
                    // tw.Write("\n");
                    // tw.Write(jsonString);
                    tw.WriteLine("{\n"+"["+Screen.width.ToString()+", "+Screen.height.ToString()+"]");
                    tw.Write("[");
                    for (int i=0; i<codedMask.Count;++i){
                        tw.Write(codedMask[i].ToString());
                        if(i+1 < codedMask.Count) tw.Write(", ");
                    }
                    tw.Write("]\n}");
                }
                
                // for(int r = 0; r < Screen.height; ++r){  // top to bottom
                //     for(int c = 0; c < Screen.width; ++c){ // left to right
                //         screenUV.x = c;
                //         screenUV.y = r;
                //         ray = myCamera.ScreenPointToRay(screenUV); // very slow
                        
                //         if (Physics.Raycast(ray, out hit)) {
                //             if(objectType.ContainsKey(hit.transform.tag)){
                //                 Debug.Log("Known tag: "+hit.transform.tag);
                //                 segmentationImage.SetPixel(c, r, new Color(0f, 0f, 0f, objectType[hit.transform.tag]/255f));
                //             } else {
                //                 Debug.Log("[ERROR]: Unknown tag: "+hit.transform.tag);
                //             }
                //         }
                //     }
                // }
                // segmentationImage.Apply();

                // byte[] imgBytes = segmentationImage.EncodeToPNG();
                // File.WriteAllBytes(scrPath, imgBytes);
                
            } else {
                Debug.Log("Finished taking photos.");
                playdatshit = false;
            }
        }
    }
    List<int> getRLEFromLines(int width, int height, Dictionary<int,Dictionary<int,Vector2Int>> lines){
        List<int> lengths = new List<int>(); // starting from zero
        int currentLength = 0;
        for(int r = 0; r < height; ++r){
            if(!lines.ContainsKey(r)){
                currentLength += width;
                // Debug.Log("no segment line: "+r);
            } else {
                // Debug.Log("segment line: "+r);
                int[] lineSegments = new int[lines[r].Keys.Count];
                lines[r].Keys.CopyTo(lineSegments, 0);
                Array.Sort(lineSegments);
                int lastBegin = 0;
                int lastLength = 0;
                foreach (int begin in lineSegments)
                {
                    currentLength += begin - lastBegin - lastLength;
                    lengths.Add(currentLength);
                    lengths.Add(0);
                    lengths.Add(lines[r][begin].x);
                    lengths.Add(lines[r][begin].y);
                    lastBegin = begin;
                    lastLength = lines[r][begin].x;
                    currentLength = 0;
                    // Debug.Log("segment line: "+r+"  column: "+begin+"  length: "+lines[r][begin].x); // ok

                }
            }
        }
        lengths.Add(currentLength);
        lengths.Add(0);
        return lengths;
    }
    Dictionary<int,Dictionary<int,Vector2Int>> rectangles2Lines1Tag(List<Vector3[]> objectRectangles){
        Dictionary<int,Dictionary<int,Vector2Int>> perLineValues= new Dictionary<int, Dictionary<int,Vector2Int>>();
        RaycastHit hit;
        Ray ray;
        Vector3 screenUV = new Vector3(Screen.height-1,0f,0f);
        foreach (Vector3[] rectangle in objectRectangles)
        {
            for(int r = (int)rectangle[0].y; r < (int)rectangle[1].y; ++r){ // ok
                bool hitLastOne = false;
                int beginHit = 0;
                int lastHitCount = 0;
                int lastTagID = 0;
                for(int c = (int)rectangle[0].x; c < (int)rectangle[1].x; ++c){
                    screenUV.x = c;
                    screenUV.y = r;
                    ray = myCamera.ScreenPointToRay(screenUV);
                    if(Physics.Raycast(ray, out hit)){
                        // if(hit.transform.CompareTag(tag)){ // for single tag
                        if(objectType.ContainsKey(hit.transform.tag)){
                            if(!hitLastOne){
                                beginHit = c;
                                lastHitCount = 0; // ok
                                lastTagID = objectType[hit.transform.tag];
                                // perLineValues[r][beginHit] = new Vector2Int(lastHitCount, objectType[hit.transform.tag]);
                                hitLastOne = true;
                            }
                            lastHitCount++;
                        } else {
                            if(hitLastOne){
                                if(!perLineValues.ContainsKey(r)){
                                    perLineValues[r] = new Dictionary<int,Vector2Int>();
                                }
                                
                                if(!perLineValues[r].ContainsKey(beginHit)){
                                    perLineValues[r][beginHit] = new Vector2Int(lastHitCount, lastTagID);
                                    // Debug.Log("row: "+r+"  col: "+beginHit+"  len: "+lastHitCount);
                                }
                                // Debug.Log("row: "+r+"  col: "+beginHit+"  len: "+lastHitCount);
                                hitLastOne = false;
                            }
                        }
                    } else {
                        if(hitLastOne){ // doesnt reach here maybe because the last point in the row it hits, so the next point cannot be missed
                            if(!perLineValues.ContainsKey(r)){
                                perLineValues[r] = new Dictionary<int,Vector2Int>();
                            }
                            if(!perLineValues[r].ContainsKey(beginHit)){
                                perLineValues[r][beginHit] = new Vector2Int(lastHitCount, lastTagID);
                                // Debug.Log("row: "+r+"  col: "+beginHit+"  len: "+lastHitCount);
                            }
                            // Debug.Log("row: "+r+"  col: "+beginHit+"  len: "+lastHitCount);
                            hitLastOne = false;
                        }
                    }
                    if(c+1 == (int)rectangle[1].x){
                        if(hitLastOne){ // doesnt reach here maybe because the last point in the row it hits, so the next point cannot be missed
                            if(!perLineValues.ContainsKey(r)){
                                perLineValues[r] = new Dictionary<int,Vector2Int>();
                            }
                            if(!perLineValues[r].ContainsKey(beginHit)){
                                perLineValues[r][beginHit] = new Vector2Int(lastHitCount, lastTagID);
                                // Debug.Log("row: "+r+"  col: "+beginHit+"  len: "+lastHitCount);
                            }
                            // Debug.Log("row: "+r+"  col: "+beginHit+"  len: "+lastHitCount);
                           hitLastOne = false;
                        }
                    }
                }
            }
        }
        return perLineValues;
    }
    List<Vector3[]> probeImage(int stepX, int stepY){
        RaycastHit hit;
        Ray ray;
        Vector3 screenUV = new Vector3(Screen.height-1,0f,0f);
        Texture2D segmentationImage = new Texture2D(Screen.width, Screen.height, TextureFormat.Alpha8, false);
        bool lastOneHit = false;
        // TODO create pairs signifying a rectangle List<Vector3[]>
        List<Vector3[]> rasterRectangles = new List<Vector3[]>();
        int lastRectangle = 0;
        for(int r = 0; r < Screen.height; r+=stepY){  // top to bottom
            for(int c = 0; c < Screen.width; c+=stepX){ // left to right
                // Debug.Log("aiming at x,y: "+c.ToString()+", "+r.ToString()); // ok
                screenUV.x = c;
                screenUV.y = r;
                ray = myCamera.ScreenPointToRay(screenUV); // very slow
                
                if (Physics.Raycast(ray, out hit)) { // this one hit but the last one didn't
                    if(objectType.ContainsKey(hit.transform.tag)){
                        if(!lastOneHit){
                            rasterRectangles.Add(new Vector3[2]);
                            rasterRectangles[lastRectangle][0] = new Vector3(Mathf.Max(c-stepX, 0f), Mathf.Max(r-stepY, 0f), 0f);
                            rasterRectangles[lastRectangle][1] = new Vector3(Screen.width, Mathf.Min(r+stepY, Screen.height), 0f);
                            // save previous x and previous y
                            // Debug.Log("Known tag: "+hit.transform.tag);
                            lastOneHit = true;
                        }
                    } else { // this one didn't hit
                        if(lastOneHit){ // but the last one did
                            rasterRectangles[lastRectangle][1].x = Mathf.Min(c+1, Screen.width);
                            ++lastRectangle;
                            lastOneHit = false;
                        }
                    }
                } else {
                    if(lastOneHit){ // but the last one did
                        rasterRectangles[lastRectangle][1].x = Mathf.Min(c+1, Screen.width);
                        ++lastRectangle;
                        lastOneHit = false;
                    }
                }
            }
        }
        return rasterRectangles;
    }
}
public class SpherePath{
    int stepsZ, stepsXY;
    float dist, dZ, dXY, minZ, maxZ;
    // assuming that path is around xyz=000
    float currentZ = 0f, currentXY = 0f;//, currentDist = 0f;
    float x, y, z;
    public Vector3[] positions;
    public int currentID = 0;
    public SpherePath(int stepsZ, int stepsXY, float dist, float minZ, float maxZ){
        this.stepsZ = stepsZ;
        this.stepsXY = stepsXY;
        this.dist = dist;
        this.dZ = (Mathf.Min(maxZ, Mathf.PI/2f)-Mathf.Max(minZ, -Mathf.PI/2f))/stepsZ;
        this.dXY = 2*Mathf.PI/stepsXY;
        this.minZ = minZ;
        this.maxZ = maxZ;
        this.currentZ = Mathf.Max(-Mathf.PI*0.5f, minZ);
        this.currentXY = 0f;
        this.x = dist*Mathf.Cos(currentXY)*Mathf.Cos(currentZ);  // from x to y onwards
        this.z = dist*Mathf.Sin(currentXY)*Mathf.Cos(currentZ);
        this.y = dist*Mathf.Sin(currentZ);  // from -z to z
        Debug.Log("currentZ:"+currentZ+"Mathf.Sin(currentZ):"+Mathf.Sin(currentZ)+"this.z:"+this.z);
        this.positions = new Vector3[stepsZ*stepsXY];
    }
    public bool generatePath(){
        bool ret = true;
        int index = 0;
        for(int i = 0; i < stepsZ; ++i){
            for(int j = 0; j < stepsXY; ++j){
                this.positions[index] = new Vector3(this.x, this.y, this.z);
                this.currentXY = this.currentXY + this.dXY;
                this.x = dist*Mathf.Cos(currentXY)*Mathf.Cos(currentZ);  // from x to y onwards
                this.z = dist*Mathf.Sin(currentXY)*Mathf.Cos(currentZ);
                ++index;
            }
            this.currentZ = Mathf.Min(Mathf.PI*0.5f, this.currentZ+this.dZ, this.maxZ);
            this.y = dist*Mathf.Sin(currentZ);  // from -z to z
            Debug.Log("this.currentZ: "+this.currentZ+"Mathf.Sin(currentZ): "+Mathf.Sin(currentZ)+"this.z:"+this.z);
        }
        return ret;
    }
    public bool next(){
        return this.currentID < this.positions.Length;
    }
    public Vector3 nextPosition(){
        return this.positions[this.currentID++];
    }
    public void reset(){
        this.currentID = 0;
    }

}
