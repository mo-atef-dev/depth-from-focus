import numpy as np
import glob
import cv2
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
input_path = "Source images/*.png"

output_path = os.path.join(script_dir, "Stacked")
images_path = os.path.join(script_dir, "Source images/*.png")
images_files = glob.glob(images_path)

scale_factor = 0.5

imgs = []
for image_path in images_files:
    img = cv2.imread(image_path)
    img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    img = cv2.resize(img, None, fx=scale_factor, fy=scale_factor)
    imgs.append(img)

# Detect and compute descriptors
sift = cv2.SIFT_create()
kps = []
descs = []

for img in imgs:
    keypoints, descriptors = sift.detectAndCompute(img, None)
    kps.append(keypoints)
    descs.append(descriptors)

# Match keypoints with the anchor image
bf = cv2.BFMatcher()
matches = []
hMatrices = []

for i in range(1, len(imgs)):
    print("Matching images " + str(i) + " and " + str(i-1))

    match = bf.match(descs[i], descs[i-1])
    match = sorted(match, key=lambda x: x.distance)
    match = match[:10]
    matches.append(match)

# Delete old images if found
for image_path in glob.glob(output_path + "/*.png"):
    os.remove(image_path)

# Save the first image
print("Saving first image")
cv2.imwrite(output_path + "/img_stacked_0.png", imgs[0])

for i in range(1, len(matches)+1):
    # Find homography
    src_pts = np.float32([ kps[i][m.queryIdx].pt for m in matches[i-1]]).reshape(-1,1,2)
    dst_pts = np.float32([ kps[i-1][m.trainIdx].pt for m in matches[i-1]]).reshape(-1,1,2)

    M, _ = cv2.findHomography(src_pts, dst_pts, cv2.RANSAC, 5.0)
    hMatrices.append(M)

    warped_img = imgs[i]

    # Warp the image
    for j, m in enumerate(hMatrices):
        warped_img = cv2.warpPerspective(warped_img, m, (imgs[0].shape[1], imgs[0].shape[0]))

    # Save the image
    print(f"Saving image {i}")
    cv2.imwrite(output_path + f"/img_stacked_{i}.png", warped_img)