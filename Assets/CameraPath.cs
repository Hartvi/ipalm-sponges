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
    public Transform focusObject;
    bool playdatshit = false;
    public GameObject[] targetObjects;
    private Dictionary<string, int> objectType;
    public int horizontalSteps = 6;
    public int verticalSteps = 5;
    public float distance = 5;
    public float minZangle = 0.2f;
    public float maxZangle = Mathf.PI/2;
    public int probeWidth = 9;
    public int probeHeight = 6;
    public string focusTagRadical = "sp";
    public string focusMaterial = "foam";
    public int focusObjectID = 13; // should be sponge
    RLElist encodedList = new RLElist();
    List<string> focusTags;
    System.Random rand = new System.Random();
    List<int[]> combsCombined;
    public bool randomObjectActivation = false;
    // public int numberOfObjects = 9;
    public bool saveBinaryMask = false;
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
                Debug.Log(t);
            }
        }
        
    }
    void Start(){
        targetObjects = FindGameObjectsWithRadicalInTag(focusTagRadical);
        List<int[]> combs3 = generateCombinations(targetObjects.Length, 3);
        List<int[]> combs2 = generateCombinations(targetObjects.Length, 2);
        List<int[]> combs1 = generateCombinations(targetObjects.Length, 1);
        combsCombined = new List<int[]>();
        for (int i = 0; i < 4; i++){
            combsCombined.AddRange(combs1);
        }
        for (int i = 0; i < 2; i++){
            combsCombined.AddRange(combs2);
        }
        for (int i = 0; i < 1; i++){
            combsCombined.AddRange(combs3);
        }

        // foreach (int[] comb in combsCombined)
        // {
        //     if(comb.Length == 3){
        //         Debug.Log("combination: "+comb[0].ToString()+comb[1].ToString()+comb[2].ToString());
        //     } else if(comb.Length == 2){
        //         Debug.Log("combination: "+comb[0].ToString()+comb[1].ToString());
        //     } else if(comb.Length == 1){
        //         Debug.Log("combination: "+comb[0].ToString());
        //     }
        // }
    }
    void Update()
    {
        if(Input.GetMouseButtonDown(0) && !playdatshit){
            playdatshit = true;
            encodedList.ims = new RLEncoding[myPath.getLength()];
            string folderPath;
            folderPath = Application.persistentDataPath + "/"+"images";
            System.IO.Directory.CreateDirectory(folderPath);
        }
        if(Input.GetMouseButtonDown(1)){
            int randomCombination = rand.Next(combsCombined.Count);
            for(int j = 0; j < targetObjects.Length;++j){
                targetObjects[j].SetActive(Array.Exists(combsCombined[randomCombination], el => el == j));
                if(Array.Exists(combsCombined[randomCombination], el => el == j)){
                    Debug.Log("Setting active "+j);
                } else {
                    Debug.Log("Setting inactive "+j);
                }
            }
            // for(int j = 0; j < combsCombined[randomCombination].Length;++j){
            //     // this is the index of combination of objects for this shot
            //     Debug.Log("Setting active object: "+j+"  tag: "+targetObjects[combsCombined[randomCombination][j]].tag);
            //     // Debug.Log("targetObjects length: "+targetObjects.Length);
            //     targetObjects[combsCombined[randomCombination][j]].SetActive(true);
            // }
            
        }
        
        if(playdatshit){
            if(myPath.next()){
                if(randomObjectActivation){
                    // for(int j = 0; j < targetObjects.Length;++j){
                    //     targetObjects[j].SetActive(false);
                    // }
                    int randomCombination = rand.Next(combsCombined.Count);
                    for(int j = 0; j < targetObjects.Length;++j){
                        targetObjects[j].SetActive(Array.Exists(combsCombined[randomCombination], el => el == j));
                    }
                    // if(combsCombined[randomCombination].Length == 3){
                    //     Debug.Log("combination: "+combsCombined[randomCombination][0].ToString()+combsCombined[randomCombination][1].ToString()+combsCombined[randomCombination][2].ToString());
                    // } else if(combsCombined[randomCombination].Length == 2){
                    //     Debug.Log("combination: "+combsCombined[randomCombination][0].ToString()+combsCombined[randomCombination][1].ToString());
                    // } else if(combsCombined[randomCombination].Length == 1){
                    //     Debug.Log("combination: "+combsCombined[randomCombination][0].ToString());
                    // } else {
                    //     Debug.Log("[ERROR] Unwanted combination length!! " + combsCombined[randomCombination].Length);
                    // }
                    // for(int j = 0; j < combsCombined[randomCombination].Length;++j){
                    //     // this is the index of combination of objects for this shot
                    //     // Debug.Log("targetObjects length: "+targetObjects.Length);
                    //     targetObjects[combsCombined[randomCombination][j]].SetActive(true);
                    //     Debug.Log("Setting active object: "+j+"  tag: "+targetObjects[combsCombined[randomCombination][j]].tag);
                    // }
                    int r = rand.Next(combsCombined[randomCombination].Length); // 1: 0; 2: 0,1; 3: 0,1,2
                    int cnt = 0;
                    // 0-7 index of object to look at
                    foreach(var obj in targetObjects){
                        if(obj.activeSelf){
                            if(cnt == r){
                                focusObject = obj.transform;
                            }
                            cnt++;
                        }
                    }
                    transform.position = focusObject.position + myPath.nextPosition();
                    transform.LookAt(focusObject.position);
                    
                } else {
                    transform.position = focusObject.position + myPath.nextPosition();
                    transform.LookAt(focusObject.position);
                }
                string scrPath, folderName = "images"+"/", imageName = myPath.currentID.ToString() + ".png";
                int maskID = myPath.currentID;
                // string maskFileName = "/SavedScreen"+myPath.currentID.ToString()+"-"+i.ToString()+".png";
                scrPath = Application.persistentDataPath + "/" + folderName + imageName;
                ScreenCapture.CaptureScreenshot(scrPath);
                // save encoding for given (object,material,image)
                encodedList.ims[myPath.currentID] = createRLEncoding4Image(folderName, imageName, maskID); 
                if(saveBinaryMask){
                    for (int i = 0; i < encodedList.ims[myPath.currentID].annotations.Count; i++)
                    {
                        Texture2D tex = RLE2alpha8(encodedList.ims[myPath.currentID].annotations[i].segmentation, true);
                        byte[] bytes = tex.EncodeToPNG();
                        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "images/"+maskID.ToString()+"-"+i.ToString()+".png"), bytes);
                    }
                }
                
            } else {
                Debug.Log("[INFO] Finished taking photos.");
                playdatshit = false;
                string jsonString = JsonUtility.ToJson(encodedList, true);
                using(TextWriter tw = new StreamWriter(Path.Combine(Application.persistentDataPath, "lre_data.json")))
                {
                    tw.Write(jsonString);
                    Debug.Log("[INFO] Saved json.");
                }
            }
        }
    }
    Texture2D RLE2alpha8(RLERaw rle, bool flipVertical){
        Texture2D texture = new Texture2D(rle.size[0], rle.size[1], TextureFormat.Alpha8, false);
        byte[] pixelBuffer = new byte[rle.size[0] * rle.size[1]];
        byte[] ret;
        int runningIndex = rle.size[0] * rle.size[1]-1;
        // rle to array
        for(int i=rle.counts.Count-1;i>-1;--i){
            if(i % 2 == 1){
                for(int j=0;j<rle.counts[i];++j){
                    pixelBuffer[runningIndex - j] = 0xFF;
                }
            }
            runningIndex -= rle.counts[i];
        }
        if(flipVertical){
            byte[] temp = new byte[pixelBuffer.Length];
            // pixelBuffer.CopyTo(temp, 0);
            for(int i=0;i<rle.size[1];++i){
                Array.Copy(pixelBuffer, i*rle.size[0], temp, pixelBuffer.Length-rle.size[0]*(i+1), rle.size[0]);
            }
            ret = temp;
        } else {
            ret = pixelBuffer;
        }
        texture.LoadRawTextureData(ret);
        texture.Apply();
        return texture;
    }
    RLEncoding createRLEncoding4Image(string folderName, string imageName, int maskID){
        RLEncoding newEncoding = new RLEncoding(); // new single encoding for one instance of (image)
        newEncoding.annotations = new List<Annotation>();
        int i = 0;
        foreach (string t in focusTags){
            // get rectangles:
            List<Vector3[]> objectRectangles = probeImage(probeWidth, probeHeight, t); // assuming simple landscape // ok
            if(objectRectangles.Count == 0){
                continue;
            }
            // get RLE for each line, {row: {start: length}} :
            Dictionary<int,Dictionary<int, int>> lineAnnotation = rectangles2Lines1Tag(objectRectangles, t);
            // get bounding box from line RLE:
            int[] boundingBox = RLELines2BoundingBox(lineAnnotation);
            // get RLE for whole image composed from RLE of each line:
            List<int> codedMask = getRLEFromLines(Screen.width, Screen.height, lineAnnotation);
            // save the RLE of image into serializable class:

            newEncoding.file_name = folderName + imageName; // image name
            newEncoding.image_id = myPath.currentID; // image name
            newEncoding.height = Screen.height; // image height
            newEncoding.width = Screen.width; // image width
            Annotation annotation = new Annotation();
            RLERaw rawRLE = new RLERaw();
            annotation.bbox = boundingBox;
            annotation.material_id = focusMaterial; // material of focus, e.g. foam
            annotation.object_id = focusObjectID; // object type, e.g. sponge
            annotation.image_id = myPath.currentID;
            annotation.bbox_mode = 0;
            annotation.mask_file = Path.Combine("images/",maskID.ToString()+"-"+i.ToString()+".png");
            rawRLE.counts = codedMask;
            rawRLE.size = new int[]{Screen.width, Screen.height};
            annotation.segmentation = rawRLE;
            newEncoding.annotations.Add(annotation);
            i++;
        }
        return newEncoding;
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
        lengths.Add(currentLength); // makes the last length exist even though it is 0
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
        if(lineNumbers.Length == 0) {
            Debug.Log("[WARNING] No lines contain RLE => no object detected?");
            return null;
        }
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
        boundingBox[2] = maxRight;
        boundingBox[3] = Screen.height - bottomRowIndex;
        return boundingBox;
    }
    Dictionary<int,Dictionary<int,int>> rectangles2Lines1Tag(List<Vector3[]> objectRectangles, string targetTag){
        Dictionary<int,Dictionary<int,int>> perLineValues= new Dictionary<int, Dictionary<int,int>>();
        RaycastHit hit;
        if(objectRectangles.Count == 0) Debug.Log("[WARNING] No object rectangles found!");
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
        // bool detectedTag = false;
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
                            // detectedTag = true;
                        }
                    } else { // this one didn't hit
                        if(lastOneHit){ // but the last one did
                            rasterRectangles[lastRectangle][1].x = c+1;
                            ++lastRectangle;
                            lastOneHit = false;
                        }
                    }
                } else {
                    if(lastOneHit){ // but the last one did
                        rasterRectangles[lastRectangle][1].x = c+1;
                        ++lastRectangle;
                        lastOneHit = false;
                    }
                }
            }
        }
        // if(!detectedTag){
        //     Debug.Log("[WARNING] Haven't detected tag: "+targetTag);
        // }
        return rasterRectangles;
    }
    public List<int[]> generateCombinations(int n, int r) {
        List<int[]> combinations = new List<int[]>();
        int[] combination = new int[r];

        // initialize with lowest lexicographic combination
        for (int i = 0; i < r; i++) {
            combination[i] = i;
        }

        while (combination[r - 1] < n) {
            combinations.Add((int[])combination.Clone());

            // generate next combination in lexicographic order
            int t = r - 1;
            while (t != 0 && combination[t] == n - r + t) {
                t--;
            }
            combination[t]++;
            for (int i = t + 1; i < r; i++) {
                combination[i] = combination[i - 1] + 1;
            }
        }
        return combinations;
    }

    GameObject[] FindGameObjectsWithRadicalInTag(string radical)
    {
        var goArray = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        var goList = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < goArray.Length; i++)
        {
            if (goArray[i].tag.Contains(radical) && ((goArray[i].transform.parent != null && !goArray[i].transform.parent.tag.Contains(radical)) || goArray[i].transform.parent == null))
            {
                goList.Add(goArray[i]);
            }
        }
        if (goList.Count == 0)
        {
            return null;
        }
        return goList.ToArray();
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
    public string file_name;
    // public int object_id; // 0=sponge,1=cube
    // public string material_id; // 0=foam
    public int width;
    public int height;
    public int image_id;
    // public int[] boundingBox;
    // public List<int> RLE;
    public List<Annotation> annotations;
}

[System.Serializable]
class RLERaw{
    
    public List<int> counts;
    public int[] size;
}
[System.Serializable]
class Annotation{
    public int[] bbox;
    public int bbox_mode;
    public int object_id;
    public string material_id;
    public int image_id;
    public string mask_file;
    public RLERaw segmentation;
}

[System.Serializable]
class RLElist{
    public RLEncoding[] ims;
}
