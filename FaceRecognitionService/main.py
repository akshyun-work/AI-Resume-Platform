from fastapi import FastAPI, UploadFile, File, HTTPException
from facenet_pytorch import MTCNN, InceptionResnetV1
from PIL import Image
import torch
import io

app = FastAPI()

device = torch.device("cpu")

mtcnn = MTCNN(
    image_size=160,
    margin=0,
    keep_all=True,
    device=device
)

resnet = InceptionResnetV1(
    pretrained="vggface2"
).eval().to(device)


@app.post("/generate-embedding")
async def generate_embedding(image: UploadFile = File(...)):
    if not image.content_type or not image.content_type.startswith("image/"):
        raise HTTPException(
            status_code=400,
            detail="Uploaded file must be an image."
        )

    image_bytes = await image.read()

    try:
        img = Image.open(io.BytesIO(image_bytes)).convert("RGB")
    except Exception:
        raise HTTPException(
            status_code=400,
            detail="Could not decode image."
        )

    # Detect and extract all faces.
    faces = mtcnn(img)

    if faces is None:
        raise HTTPException(
            status_code=400,
            detail="No face detected."
        )

    if faces.ndim == 3:
        faces = faces.unsqueeze(0)

    if faces.shape[0] > 1:
        raise HTTPException(
            status_code=400,
            detail="Multiple faces detected. Only one face is allowed."
        )

    face = faces.to(device)

    with torch.no_grad():
        embedding = resnet(face)

    embedding = embedding.cpu().numpy()[0]

    return {
        "embedding": embedding.tolist()
    }