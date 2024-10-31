# PixelArtPipeline

Based on [PixelArtPipeline](https://github.com/Broxxar/PixelArtPipeline) of [Broxxar](https://github.com/Broxxar)

In editor tool to render 3D object as sprite sheet or single sprite with normal map — based on the character art pipeline of the [Dead Cells](https://dead-cells.com/)

# Functionality
- Capture a 3D object with legacy animation into a sprite sheet (future support for modern animations planned)
- Set count of the frames per second to the capture of animation
- Set start and end frames of your animation
- Preview animation (not result of capture)
- Capture a single frame from the camera’s view
- Set sprites resolution (named as "Cell Size")

![image](https://github.com/user-attachments/assets/d88be797-03e2-4d7e-9fa6-0b6982df9009)

# How to use
- Navigate to Assets/PixelArtPipeline/Demo/Scenes/CaptureScene.unity to explore and test the setup

or

- Add your 3D model and a camera to the scene (camera will be used for capturing).
- Add the PixelArtPipelineCapture script to any object in the scene.
- Set references into the PixelArtPipelineCapture (capture camera, target 3D object, and animation if it needed)
- Press one of the buttons:
  - "Capture Screen" (capture single sprite from the camera’s view)
  - "Capture Animation"

Tip: In most cases i think you prefer use unlit materials on your 3D object without any lighting on the scene. However, any materials and light setup can be used based on your preferences.

# Exemple

![Source](https://github.com/user-attachments/assets/7491dcde-7629-4aab-b6d0-b7dd1b7bc062)
![Result](https://github.com/user-attachments/assets/27f68948-f6b6-478a-a757-05029921596f)
