// using System.Collections;
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
    public int horizontalSteps = 6;
    public int verticalSteps = 5;
    public float distance = 5;
    public float minZangle = -0.2f;
    public float maxZangle = Mathf.PI/2;
    public int probeWidth = 9;
    public int probeHeight = 6;
    public string focusTagRadical = "sp";
    public string focusMaterial = "foam";
    public int focusObjectID = 13; // should be sponge
    RLElist encodedList = new RLElist();
    List<string> focusTags;
    
    void Awake()
    {
        myCamera = gameObject.GetComponent<Camera>();
        myPath = new SpherePath(horizontalSteps, verticalSteps, distance, minZangle, maxZangle);
        myPath.generatePath();
        objectType = new Dictionary<string, int>();

        focusTags = new List<string>();
        foreach (string t in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if(t.Contains(focusTagRadical) && GameObject.FindWithTag(t) != null){
                focusTags.Add(t); // active objects with wanted tag
            }
        }
    }
    void Update()
    {
        if(Input.GetMouseButtonDown(0) && !playdatshit){
            playdatshit = true;
            encodedList.ims = new RLEncoding[myPath.getLength()*focusTags.Count];
        }
        if(playdatshit){
            if(myPath.next()){
                transform.position = focusObject.position + myPath.nextPosition();
                transform.LookAt(focusObject.position);
                string scrPath;
                scrPath = Application.persistentDataPath + "/" + "pic" + myPath.currentID.ToString() + ".png";
                ScreenCapture.CaptureScreenshot(scrPath);
                int i = 0;
                foreach (string t in focusTags){
                    // get rectangles:
                    List<Vector3[]> objectRectangles = probeImage(probeWidth, probeHeight, t); // assuming simple landscape // ok
                    // get RLE for each line, {row: {start: length}} :
                    Dictionary<int,Dictionary<int, int>> lineAnnotation = rectangles2Lines1Tag(objectRectangles, t);
                    // get bounding box from line RLE:
                    int[] boundingBox = RLELines2BoundingBox(lineAnnotation);
                    Debug.Log("topLeft: "+boundingBox[0]+"  bottomRight: "+boundingBox[1]);//
                    // get RLE for whole image composed from RLE of each line:
                    List<int> codedMask = getRLEFromLines(Screen.width, Screen.height, lineAnnotation);
                    // save the RLE of image into serializable class:
                    RLEncoding newEncoding = new RLEncoding(); // new single encoding for one instance of (object,material,image)
                    newEncoding.imageID = "seg" + myPath.currentID.ToString()+".png"; // image name
                    newEncoding.imageHeight = Screen.height; // image height
                    newEncoding.imageWidth = Screen.width; // image width
                    newEncoding.materialID = focusMaterial; // material of focus, e.g. foam
                    newEncoding.objectID = focusObjectID; // object type, e.g. sponge
                    newEncoding.RLE = codedMask; // the actual RLE encoding for a single (object,material)
                    newEncoding.boundingBox = boundingBox;
                    encodedList.ims[myPath.currentID*focusTags.Count+(i++)] = newEncoding; // save encoding for given (object,material,image)
                }
                
            } else {
                Debug.Log("Finished taking photos.");
                playdatshit = false;
                string jsonString = JsonUtility.ToJson(encodedList, true);
                using(TextWriter tw = new StreamWriter(Application.persistentDataPath + "/" + "lre_data" + ".json"))
                {
                    tw.Write(jsonString);
                    Debug.Log("Saved json");
                }
            }
        }
    }
    List<int> getRLEFromLines(int width, int height, Dictionary<int,Dictionary<int,int>> lines){
        int totalLength = 0;
        List<int> lengths = new List<int>(); // starting from zero
        int currentLength = 0;
        for(int r = height-1; r > -1; --r){
            if(!lines.ContainsKey(r)){
                currentLength += width;
            } else {
                int[] lineSegments = new int[lines[r].Keys.Count];
                lines[r].Keys.CopyTo(lineSegments, 0);
                Array.Sort(lineSegments);
                int lastBegin = 0;
                int lastLength = 0;
                for (int i = 0; i < lineSegments.Length;++i)// begin in lineSegments)
                {
                    currentLength += lineSegments[i] - lastBegin - lastLength;
                    if(currentLength > 0){
                        lengths.Add(currentLength);
                    } else {
                    }
                    totalLength += currentLength; // parallel counting
                    if(lines[r][lineSegments[i]] > 0) {
                        lengths.Add(lines[r][lineSegments[i]]); // take into account that I haven't reached the end of the line
                    } else {
                    }
                    totalLength += lines[r][lineSegments[i]]; // parallel counting
                    // previous round \/
                    lastBegin = lineSegments[i];
                    lastLength = lines[r][lineSegments[i]];
                    if(i+1 == lineSegments.Length){
                        currentLength = (width - (lineSegments[i] + lines[r][lineSegments[i]]));
                    } else {
                        currentLength = 0;
                    }

                }
            }
        }
        lengths.Add(currentLength);
        totalLength += currentLength; // parallel counting
        if(totalLength != Screen.height*Screen.width){
            Debug.Log("[ERROR] RLE sum not checking up. RLE sum: "+totalLength+"  vs: "+(Screen.height*Screen.width));
        }
        return lengths;
    }
    int[] RLELines2BoundingBox(Dictionary<int,Dictionary<int, int>> lineAnnotation){
        int[] boundingBox = new int[4];
        int[] lineNumbers = new int[lineAnnotation.Keys.Count];
        lineAnnotation.Keys.CopyTo(lineNumbers, 0);
        Array.Sort(lineNumbers);
        int topRowIndex = lineNumbers[lineNumbers.Length-1]; // unity goes from bottom to top; take top row
        int bottomRowIndex = lineNumbers[0]; // unity goes from bottom to top; take bottom row
        int numberOfTopKeys = lineAnnotation[topRowIndex].Keys.Count; // how many beginnings there are on this row
        int[] topLineStarts = new int[numberOfTopKeys]; // array for holding all starts of positive regions
        int maxLeft = Int32.MaxValue;
        int maxRight = 0;
        foreach (KeyValuePair<int,Dictionary<int,int>> row in lineAnnotation)
        {
            foreach (KeyValuePair<int,int> startnLength in row.Value)
            {
                if(startnLength.Key < maxLeft){
                    maxLeft = startnLength.Key; // start can be between 0 and Screen.width-2
                }
                if(startnLength.Value + startnLength.Key > maxRight){
                    maxRight = startnLength.Value + startnLength.Key; // (start + length) makes it go right
                }
            }
        }
        
        boundingBox[0] = maxLeft;
        boundingBox[1] = Screen.height - topRowIndex;
        boundingBox[2] = maxLeft;
        boundingBox[3] = Screen.height - bottomRowIndex;
        return boundingBox;
    }
    Dictionary<int,Dictionary<int,int>> rectangles2Lines1Tag(List<Vector3[]> objectRectangles, string targetTag){
        Dictionary<int,Dictionary<int,int>> perLineValues= new Dictionary<int, Dictionary<int,int>>();
        RaycastHit hit;
        Ray ray;
        Vector3 screenUV = new Vector3(Screen.height-1,0f,0f);
        foreach (Vector3[] rectangle in objectRectangles)
        {
            for(int r = (int)rectangle[0].y; r < (int)rectangle[1].y; ++r){ // ok
                bool hitLastOne = false;
                int beginHit = 0;
                int lastHitCount = 0;
                for(int c = (int)rectangle[0].x; c < (int)rectangle[1].x; ++c){
                    screenUV.x = c;
                    screenUV.y = r;
                    ray = myCamera.ScreenPointToRay(screenUV);
                    if(Physics.Raycast(ray, out hit)){
                        if(hit.transform.tag == targetTag){
                            if(!hitLastOne){
                                beginHit = c;
                                lastHitCount = 0; // ok
                                hitLastOne = true;
                            }
                            lastHitCount++;
                        } else {
                            if(hitLastOne){
                                if(!perLineValues.ContainsKey(r)){
                                    perLineValues[r] = new Dictionary<int,int>();
                                }
                                
                                if(!perLineValues[r].ContainsKey(beginHit)){
                                    perLineValues[r][beginHit] = lastHitCount;
                                }
                                hitLastOne = false;
                            }
                        }
                    } else {
                        if(hitLastOne){ // doesnt reach here maybe because the last point in the row it hits, so the next point cannot be missed
                            if(!perLineValues.ContainsKey(r)){
                                perLineValues[r] = new Dictionary<int,int>();
                            }
                            if(!perLineValues[r].ContainsKey(beginHit)){
                                    perLineValues[r][beginHit] = lastHitCount;
                            }
                            hitLastOne = false;
                        }
                    }
                    if(c+1 == (int)rectangle[1].x){
                        if(hitLastOne){ // doesnt reach here maybe because the last point in the row it hits, so the next point cannot be missed
                            if(!perLineValues.ContainsKey(r)){
                                perLineValues[r] = new Dictionary<int,int>();
                            }
                            if(!perLineValues[r].ContainsKey(beginHit)){
                                perLineValues[r][beginHit] = lastHitCount;
                            }
                           hitLastOne = false;
                        }
                    }
                }
            }
        }
        return perLineValues;
    }
    List<Vector3[]> probeImage(int stepX, int stepY, string targetTag){
        RaycastHit hit;
        Ray ray;
        Vector3 screenUV = new Vector3(Screen.height-1,0f,0f);
        Texture2D segmentationImage = new Texture2D(Screen.width, Screen.height, TextureFormat.Alpha8, false);
        bool lastOneHit = false;
        List<Vector3[]> rasterRectangles = new List<Vector3[]>();
        int lastRectangle = 0;
        for(int r = 0; r < Screen.height; r+=stepY){  // top to bottom
            for(int c = 0; c < Screen.width; c+=stepX){ // left to right
                screenUV.x = c;
                screenUV.y = r;
                ray = myCamera.ScreenPointToRay(screenUV); // very slow
                
                if (Physics.Raycast(ray, out hit)) { // this one hit but the last one didn't
                    if(hit.transform.tag == targetTag){
                        if(!lastOneHit){
                            rasterRectangles.Add(new Vector3[2]);
                            rasterRectangles[lastRectangle][0] = new Vector3(Mathf.Max(c-stepX, 0f), r, 0f);
                            rasterRectangles[lastRectangle][1] = new Vector3(Screen.width, Mathf.Min(r+stepY, Screen.height), 0f);
                            // save previous x and previous y
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
    public int currentID = -1;
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
        // Debug.Log("currentZ:"+currentZ+"Mathf.Sin(currentZ):"+Mathf.Sin(currentZ)+"this.z:"+this.z);
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
            // Debug.Log("this.currentZ: "+this.currentZ+"Mathf.Sin(currentZ): "+Mathf.Sin(currentZ)+"this.z:"+this.z);
        }
        return ret;
    }
    public bool next(){
        return this.currentID+1 < this.positions.Length;
    }
    public Vector3 nextPosition(){
        return this.positions[++this.currentID];
    }
    public void reset(){
        this.currentID = -1;
    }
    public int getLength(){
        return this.stepsXY*this.stepsZ;
    }

}

[System.Serializable]
class RLEncoding{ // , ISerializationCallbackReceiver
    public string imageID;
    public int objectID; // 0=sponge,1=cube
    public string materialID; // 0=foam
    public int imageWidth;
    public int imageHeight;
    public int[] boundingBox;
    public List<int> RLE;
}

[System.Serializable]
class RLElist{
    public RLEncoding[] ims;
}

[Serializable]
enum ObjectType{
    sponge,
    cube,
    die
}

