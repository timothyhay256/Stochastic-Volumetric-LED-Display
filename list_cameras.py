import cv2

def test_cameras(max_index=10):
    working_cameras = []
    print("Testing camera indices from 0 to", max_index)
    
    for index in range(0, max_index + 1):
        cap = cv2.VideoCapture(index)
        if cap.isOpened():
            ret, frame = cap.read()
            if ret:
                print(f"Camera {index} is working.")
                working_cameras.append(index)
            else:
                print(f"Camera {index} opened but failed to read frame.")
            cap.release()
        else:
            print(f"Camera {index} is not available.")

    print("\nWorking camera indices:", working_cameras)
    return working_cameras

if __name__ == "__main__":
    test_cameras()
