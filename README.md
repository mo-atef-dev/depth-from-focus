# Depth from Focus

A C# WPF GUI application to study obtaining depth information from multiple images at different focus distances.

## Table of contents

- [Depth from Focus](#depth-from-focus)
  - [Table of contents](#table-of-contents)
  - [Theory](#theory)
    - [Introduction](#introduction)
    - [Depth from focus](#depth-from-focus-1)
    - [Focus measurement](#focus-measurement)
      - [Window size](#window-size)
      - [Focus measurement method](#focus-measurement-method)
  - [Result](#result)
    - [Computer generated scene (no stacking)](#computer-generated-scene-no-stacking)
    - [Computer generated scene (stacking)](#computer-generated-scene-stacking)
    - [Real world scene](#real-world-scene)
  - [Building](#building)
  - [Application](#application)
  - [Note on project architecture](#note-on-project-architecture)
  - [Appendix A: Focus measurement analysis](#appendix-a-focus-measurement-analysis)
    - [Modified laplacian](#modified-laplacian)
    - [Variance](#variance)
    - [Gradient norm](#gradient-norm)
    - [Conclusion](#conclusion)
  - [Appendix B: Stacking](#appendix-b-stacking)

## Theory

### Introduction

When taking an image using a lens camera, a point may appear as circle on the sensor rather than a single pixel. This effect is known as defocus aberration. [^1]

This defocus blur occurs because the image of a point is formed either in front or behind of the image sensor.

![Figure 1](/Images/img_lens_blur.png)

As shown in the image, light coming from point at distance `S1` from the lens is converged by the lens at exactly a single point on the image sensor, while light coming from point at `S2` covers a region of diameter `c` on the sensor (it is focused slightly before the sensor). Thus, each point of an object at depth `S1` will result in a response of a single point on the sensor, and the object will appear sharp. While for objects at distance `S2`, each point will cause a circle response on the sensor, making the object look blurry. [^2]

![Figure 2](https://upload.wikimedia.org/wikipedia/commons/thumb/3/3f/Circles_of_confusion_lens_diagram.svg/1280px-Circles_of_confusion_lens_diagram.svg.png)

Here is another image that shows how points at different distances cause circular response of various diameters on the sensor.

Then we can conclude that the depth information of an object is encoded in how defocused (blurred) it is.

Of course, this is just a simple explanation. In reality, it is not a single depth plane that is exactly in focus, but rather a continuous depth range, known as *depth of field*. [^3] However, for simplicity, we assume that only a single plane at a specific depth is in focus.

There are multiple ways to obtain depth information from defocus information. In this project, depth information is obtained by taking multiple images at different focus lengths.

### Depth from focus

Take a look at the below two computer generated images of the same scene (same camera position and same objects).

![Near focus image](/Images/img_near_focus.png)
![Far focus image](/Images/img_far_focus.png)

The image at the top is focused at 50cm from the camera. The image at the bottom is focused at 120cm. For reference, the wall at the background is at a depth of 160cm.

Notice how in the picture focused at 50cm, the block near the camera is sharp, while objects in the background (including the wall) are blurry. In the second image (focused at 120cm),the far block is sharp while the close block is blurry.

If we take multiple images of the same scene, with each image being focused at a different distance, we can determine the distance of each object from the camera by determining in which image it looks the sharpest.

So for example if we have the following images at different focus distances

![Image stack](/Images/img_images.png)

This is in principal is like scanning the scene using different focus distances. Each image will have the plane at the focus depth, which we can call the focus plane, being the sharpest.

![Scanning through the focus planes](/Images/img_focus_scan.png)

If we examine the wooden monkey object at the images taken (bellow only three images are shown for illustration), the distance of the monkey will be at the distance where it is sharpest.

![Different focus for each image](/Images/img_focuses.png)

In the image shown above, it is clearly sharpest at 90cm, compared to 30cm and 160cm.

Of course, since objects are 3D and depth varies for different points on the same object, we should measure how each pixel in the image is focused. The question is __"How do we measure the focus strength of each pixel in the images?"__ We will get to that in the next section.

The image below demonstrate how the depth of a point is determined by the image which has the highest focus measurement for the point.

![Finding closest focus plane](/Images/img_focus_measure.png)

Since the images are taken in discrete steps, we will have errors in our depth estimation due to quantization. Assuming we are able to measure focus with certainty, the error will look something like this:

![Quantization error](/Images/img_quant_error.png)

This error can be reduced by taking more images at smaller steps, but this will require more image capture time, computation time and memory.

We can reduce the error further by interpolating the focus measurements, to obtain an estimated distance of where the point would have actually been in best focus. This is shown in the image below.

![Interpolation](/Images/img_focus_interpolation.png)

A complete and formal study should include determining the best model for interpolation (quadratic, cubic, Gaussian ... etc) based on how much blur (the blur circle diameter) varies with depth and focus distance, and its relation with the focus measurement method (see next section). However, due to time constraints, interpolation by fitting a Gaussian is arbitrarily used in this project.

For more information regarding the math and theory check [^5].

### Focus measurement

Of course, the RGB or value of a pixel doesn't give any indication of how much it is sharp. We need to take into consideration a window of the surrounding pixels to measure the level of focus.

Two factors should be taken in consideration:

- The size of the window around the pixel of interest
- The method used for focus measurement

Measuring focus as analogous to measuring the sharpness of the image patch in the window. But this is only in the case of the scene being textured. For non textured or smooth surfaces, being in focus doesn't make much difference in terms of sharpness.

This is the most significant constraint we have when using this method.
> To obtain depth from focus, the scene surfaces must be textured

We will explore how to measure the focus (sharpness) later, but for now we will explore the effect of the window size taken around a pixel for focus measurement.

#### Window size

Large window size leads to larger error in sloped or curved surfaces, and at object edges, since large number of pixels at different depths are taken into consideration. Making the window size smaller will reduce this error, since the closest pixels are more likely to be at the same depth. But surface texture will may not be visible at small window sizes. In addition, noise resulting from the camera system will be more significant at small window sizes. And if any filters are used to remove the noise, small scene textures may be removed at the process, making them not visible for small window sizes.

![Different window sizes](/Images/img_window_size.png)

In the image above, we can see that for the small window size (red square), there is not much difference between near focus and far focus images, as the texture of the surface at that window is large compared to the window size. By taking a larger window size (green square), more variation is visible between the two images.

Thus, when choosing a window size, we follow this rule:
> Choose the smallest window size possible that is larger than:
>
> - Inherent scene textures
> - Any noise removal filters applied on the images

#### Focus measurement method

A good focus measurement method should ideally have the following properties:

- Provide higher values for textured image patches
- Not susceptible to noise
- Consistent for multiple texture sizes
- Provides good measurements in light and dark areas
- Sensitive to small variations in smoothing (can detect even small changed in texture intensity)
- Direct function of blur intensity (good mapping between them leads to better interpolation)

In the application, I use the sum of the gradient norm to measure focus level.

More details are given in [Appendix A](#appendix-a-focus-measurement-analysis) regarding comparing the different methods, and how to further improve such comparison.

## Result

### Computer generated scene (no stacking)

The scene is shown below

![CGI scene](/Images/img_far_focus.png)

Scene camera settings:

- F-stop = 1.8
- Output resolution 960x540 pixel
- Focal length 50mm

Parameters used:

- 14 images were taken, starting from focus at 30cm until 160cm (10 cm increments between each two consecutive images).
- The focus measurement window is taken as 9x9 pixels.
- The method used is gradient norm.
- No noise removal filters are used (no noise was added in the images)
- A median filter of size 9x9 was applied as post processing before outputting the depth map.

The following results is obtained for the filtered estimated depth map:

![CGI scene depth](/Images/img_depth.png)

The resulting error (compared to the actual depth) normalized:

![CGI scene error](/Images/img_error_normalized.png)

And the error histogram:
![CGI scene error histogram](/Images/img_error_hist.png)

Notice the largest concentration of errors is around the edges of the objects. A fair amount of error also exists in the background wall, mainly because of its large texture compared to the window size.

### Computer generated scene (stacking)

For the computer generated scene below, the focal length with set to change when changing the distance at focus. This is done to imitate an effect known as *focal breathing*.[^4]

The images have to be stacked before estimating the depth, introducing more errors because of misalignment.

![CGI scene with focus breathing](/Images/img_scene_stack.png)

Scene camera settings:

- F-stop = 1.8
- Output resolution 960x540 pixel
- Focal length 50mm (when focused at infinity)

Parameters used:

- 14 images were taken, starting from focus at 30cm until 160cm (10 cm increments between each two consecutive images).
- The focus measurement window is taken as 9x9 pixels.
- The method used is gradient norm.
- No noise removal filters are used (no noise was added in the images)
- A median filter of size 9x9 was applied as post processing before outputting the depth map.

The following results is obtained for the filtered estimated depth map:

![CGI scene with focus breathing depth](/Images/img_depth_stack.png)

The resulting error (compared to the actual depth) normalized:

![CGI scene with focus breathing error](/Images/img_error_stack.png)

And the error histogram:
![CGI scene with focus breathing error histogram](/Images/img_error_hist_stack.png)

Here the error is amplified significantly due to errors in stacking. Of course, the algorithm used to stack the images is very simple and better algorithms would result in better results.

### Real world scene

The next scene is a real world scene taken by an Oppo Reno 3 phone camera. The image size is scaled to 1200x1200.

![Real world scene](/Images/img_real.png)

Parameters used:

- 14 images were taken, starting from focus at 10cm until 70cm (5cm increments between each two consecutive images).
- The focus measurement window is taken as 25x25 pixels.
- The method used is gradient norm.
- 5x5 Gaussian smoothing filter is used to remove noise.
- A median filter of size 25x25 was applied as post processing before outputting the depth map.

The following results is obtained for the filtered estimated depth map:

![Real world scene](/Images/img_real_depth.png)

## Building

Use Visual Studio or dotnet on Windows to build the .sln file in the repository.

## Application

The application itself is a C# WPF program. It is straight forward to use.

It consists of two tabs.

![Main view](/Images/img_app_main.png)

The "Main view" tab has the following sections:

- Image list: Shows the currently loaded images
- Main image: Shows the currently selected image from the image list.
- True depth image: If a true depth map is loaded, it is shown here (normalized).
- Settings: Includes parameters of the depth measurement operation.
- Focus measurements: After performing depth calculation (by pressing the button), this section shows a bar graph of the focus measurements, when the user clicks on a pixel in the "Main image" section. It is basically a debugging tool to investigate focus measurements of specific pixels.

![Depth view](/Images/img_app_depth.png)

The "Depth map" tab has the following sections:

- Normalized depth map: Displays the normalized depth map (after calculation).
- Normalized error map: If a true depth map was loaded, this section shows the normalized error (difference between the calculated depth and true depth).
- Error statistics: If a true depth map is loaded, this section shows error statistics and error histogram.
- Settings: This section has the post filtering settings (type and kernel size) as well as a button that recalculates filtering (to change it without recalculating depth).

At the bottom the buttons describe themselves.

## Note on project architecture

The application doesn't use advanced GUI architectures such as MVVM. The reasoning behind this (despite WPF having great support for data binding and MVVM) is that the UI functionality is simple, and handling it directly through controls' events handlers is enough.

Data binding is sometimes used though when it is more convenient (such as updating the selected image).

## Appendix A: Focus measurement analysis

I have experimented with the following methods:

- Modified laplacian
- Variance
- Norm of gradient

To compare these methods, I use 4 randomly generated image patches:

- One is textured and light
- One is textured and dark
- One is soft and light
- One is soft and dark

Each patch is blurred at various kernel sizes to imitate being out of focus. Before measuring, Gaussian noise is added to the image (after the blurring), to simulate noise in real camera system. A noise removal kernel is then used as a pre-measuring step to simulate practical attempts of noise removal.

Then we obtain the focus measurements using the method under consideration, normalize the results, and then preview them.

### Modified laplacian

Modified laplacian is the sum of the absolute second spatial partial derivatives at the window.

$$
\sum_{W} \left( \left| \frac{\partial^2 I}{\partial x^2} \right| + \left| \frac{\partial^2 I}{\partial y^2} \right| \right)
$$

![Modified laplace method](/Images/img_modified_laplacian.svg)

### Variance

Variance is measured by taking the sum of the square difference between each pixel of the image patch and the mean of all pixels in the patch.

$$
\sum_{W} \left( I - I_{\text{avg}} \right)^2
$$

![Variance method](/Images/img_variance.svg)

### Gradient norm

Norm of gradient method is the sum of the norm (usually here the first norm) of the image gradient in the patch.

$$
\sum_{W} \left( \left| \frac{\partial I}{\partial x} \right| + \left| \frac{\partial I}{\partial y} \right| \right)
$$

![Gradient norm method](/Images/img_gradient_norm.svg)

### Conclusion

The bar under each image indicates the normalized obtained focus measurement of the given method.

As can be seen, variance and gradient norm produce good results, since the measurement decreases as the blur kernel size increases, for all image patches.

As for modified laplacian, significant randomness exists in the resulting measurements.

Again, time limitations prevented studying these methods (and more) with greater details.
Here are some suggestions to gain more insight:

- Try more scenarios like spatially varying blur (found in edges, curved surfaces, and planar surfaces not normal to the camera axis).
- Try weighted measurements (pixels near the center contribute more to the measurement).
- Using adaptive window size depending on the patch properties.
- Using adaptive methods (different or multiple methods) to provide better measurement.

## Appendix B: Stacking

A python script `stackImages.py` contains a very basic algorithm to stack images before using them in the application. It loads the images in the folder called `Source images` in its directory and outputs the stacked images in the folder `Stacked`.

It stacks the images by finding the homographic transform between each image and the one before it, then applying for each image the sequence homographic transforms that precede it. So for image in index i, (i-1) transforms are performed.

At the end, all images should be aligned with the first image.

This is by no means the best way to stack the images (for example if the camera is stable, we can use a similarity transform instead of a homographic one to obtain a better result), but it was put up quickly to test the depth estimation algorithm with real world images, which must be stacked.

[^1]: [https://en.wikipedia.org/wiki/Defocus_aberration](https://en.wikipedia.org/wiki/Defocus_aberration)
[^2]: [https://en.wikipedia.org/wiki/Circle_of_confusion](https://en.wikipedia.org/wiki/Circle_of_confusion)
[^3]: [https://en.wikipedia.org/wiki/Depth_of_field](https://en.wikipedia.org/wiki/Depth_of_field)
[^4]: [https://toptech.news/what-is-focus-breathing-in-a-camera-lens/](https://toptech.news/what-is-focus-breathing-in-a-camera-lens/)
[^5]: [https://www.youtube.com/playlist?list=PL2zRqk16wsdowTcMVNhV0-7RjSOBS4rHO](https://www.youtube.com/playlist?list=PL2zRqk16wsdowTcMVNhV0-7RjSOBS4rHO)
