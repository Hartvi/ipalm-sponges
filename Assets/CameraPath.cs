using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
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
        myPath = new SpherePath(1, 1, 3f, -0.1f, Mathf.PI/2);
        myPath.generatePath();
        objectType = new Dictionary<string, int>();
        // TEMPORARY
        objectType["background"] = 0;
        objectType["sponge"] = 1;
        objectType["die"] = 2;
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
                RaycastHit hit;
                Ray ray;
                Vector3 screenUV = new Vector3(Screen.height-1,0f,0f);
                Texture2D segmentationImage = new Texture2D(Screen.width, Screen.height, TextureFormat.Alpha8, false);
                // ray = myCamera.ScreenPointToRay(screenUV);
                // if (Physics.Raycast(ray, out hit)) {
                //     if(objectType.ContainsKey(hit.transform.tag)){
                //         Debug.Log("Known tag: "+hit.transform.tag);
                //         segmentationImage.SetPixel(0, Screen.height-1, new Color(0f, 0f, 0f, objectType[hit.transform.tag]/255f));
                //     } else {
                //         Debug.Log("[ERROR]: Unknown tag: "+hit.transform.tag);
                //     }
                // }
                List<Vector3> objectRectangles = probeImage(7, 5);
                foreach (Vector3 rectangle in objectRectangles)
                {
                    Debug.Log(rectangle);
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
                segmentationImage.Apply();
                // Debug.Log("pixel in the middle: "+segmentationImage.GetPixel(Screen.width/2, Screen.height/2));
                // byte[] imgBytes = segmentationImage.EncodeToPNG();
                // File.WriteAllBytes(scrPath, imgBytes);
                
            } else {
                Debug.Log("Finished taking photos.");
                playdatshit = false;
            }
        }
    }
    List<Vector3> probeImage(int stepX, int stepY){
        RaycastHit hit;
        Ray ray;
        Vector3 screenUV = new Vector3(Screen.height-1,0f,0f);
        Texture2D segmentationImage = new Texture2D(Screen.width, Screen.height, TextureFormat.Alpha8, false);
        bool lastOneHit = false;
        // TODO create pairs signifying a rectangle List<Vector3[]>
        List<Vector3> rasterRectangles = new List<Vector3>();        
        for(int r = 0; r < Screen.height; r+=stepY){  // top to bottom
            for(int c = 0; c < Screen.width; c+=stepX){ // left to right
                // Debug.Log("aiming at x,y: "+c.ToString()+", "+r.ToString()); // ok
                screenUV.x = c;
                screenUV.y = r;
                ray = myCamera.ScreenPointToRay(screenUV); // very slow
                
                if (Physics.Raycast(ray, out hit)) { // this one hit but the last one didn't
                    if(objectType.ContainsKey(hit.transform.tag)){
                        if(!lastOneHit){
                            rasterRectangles.Add(new Vector3(c-stepX, r-stepY, 0f));
                            // save previous x and previous y
                            // Debug.Log("Known tag: "+hit.transform.tag);
                        }
                        lastOneHit = true;
                    } else { // this one didn't hit
                        if(lastOneHit){ // but the last one did
                            rasterRectangles.Add(new Vector3(c, r+stepY, 0f));
                        }
                        lastOneHit = false;
                    }
                } else {
                    if(lastOneHit){ // but the last one did
                        rasterRectangles.Add(new Vector3(c, r+stepY, 0f));
                    }
                    lastOneHit = false;
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
