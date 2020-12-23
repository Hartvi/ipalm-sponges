from PIL import Image  
import os

list_of_pngs = list()
for f in os.listdir(os.getcwd()):
    if f[-4:] == ".png":
        list_of_pngs.append(f)

for f in list_of_pngs:

    im = Image.open(f)  

    # Size of the image in pixels (size of orginal image)  
    # (This is not mandatory)  
    width, height = im.size  
    
    newsize = (width+1, height) 
    im1 = im.resize(newsize)
    im1.save(f)

