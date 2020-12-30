# ipalm-sponges
Generating training data for sponges, soft dice in the [ipalm project](https://sites.google.com/view/ipalm). 

Under guidance of [Matej Hoffmann](https://sites.google.com/site/matejhof/).

## How to use
Basically only one script CameraPath.cs attached to the MainCamera.
### Main Camera <- CameraPath.cs
- `Horizontal Steps` - How many steps to take around `Focus Object` between angles `0` and `2*PI`.
- `Vertical Steps` - How many steps to take from `Min Z Angle` to `Max Z Angle` bottom to top.
- `Distance` - Distance in Unity meters from the object the camera's looking at.
- `Min Z Angle` - `-PI/2` corresponds to going directly below the `Focus Object`.
- `Max Z Angle` - `PI/2` corresponds to going directly abve the `Focus Object`.
- `Probe Width Half` - Half of the width of the 'probe', that scans the current view for the presence of objects with tag containing `Focus Tag Radical` .
- `Probe Height Half` - Same for the height.
  - Probe as of now scans nine points around the perimeter of the specified rectangle `(point.x +- Probe Width Half, point.y +- Probe Height Half)`
- `Focus Tag Radical` - Part of the tag to be matched when making the RLE and binary masks.
  - As of now **line-wise** RLE is **always** created. -> **TODO**.
- `Random Object Activation` - Set active 1 to 3 objects out of however many have been found to have `Focus Tag Radical` in their tag.
  - Randomly have camera look at one of them.
- `Save Binary Mask` - Save binary mask in alpha channel of PNG in addition to the line-wise RLE.


## TODO
- [ ] Make optional RLE writing to JSON
- [ ] Column-wise RLE
- [ ] Customizable `Random Object Activation`
- [ ] Save JSON bit by bit, eats up to an inifiniti amount of RAM
  - Data for 1.4k pictures ~ 8GB RAM
- [ ] Some might say use shaders
  - Others exist on GitHub, but they I couldn't get them to work on my PC.
  
