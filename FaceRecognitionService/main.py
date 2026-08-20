from fastapi import FastAPI, UploadFile, File, HTTPException

app = FastAPI()


@app.post("/generate-embedding")
async def generate_embedding(image: UploadFile = File(...)):
    if not image.content_type or not image.content_type.startswith("image/"):
        raise HTTPException(
            status_code=400,
            detail="Uploaded file must be an image."
        )

    # Temporary dummy embedding.
    # We'll replace this with MTCNN + alignment + embedding generation.
    embedding = [0.1, 0.2, 0.3, 0.4]

    return {
        "embedding": embedding
    }