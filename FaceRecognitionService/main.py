from fastapi import FastAPI, UploadFile, File, HTTPException
from facenet_pytorch import MTCNN, InceptionResnetV1
from PIL import Image
import torch
import io
import os
import json
import faiss
import numpy as np
from pydantic import BaseModel

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


INDEX_DIRECTORY = "face_index"
INDEX_PATH = os.path.join(INDEX_DIRECTORY, "faces.index")
CANDIDATE_IDS_PATH = os.path.join(
    INDEX_DIRECTORY,
    "candidate_ids.json"
)

EMBEDDING_DIMENSION = 512
HNSW_M = 32

os.makedirs(INDEX_DIRECTORY, exist_ok=True)


def create_index():
    index = faiss.IndexHNSWFlat(
        EMBEDDING_DIMENSION,
        HNSW_M
    )

    # Better search accuracy.
    index.hnsw.efSearch = 64

    return index


def save_index():
    faiss.write_index(index, INDEX_PATH)

    with open(CANDIDATE_IDS_PATH, "w") as file:
        json.dump(candidate_ids, file)


if (
    os.path.exists(INDEX_PATH)
    and os.path.exists(CANDIDATE_IDS_PATH)
):
    index = faiss.read_index(INDEX_PATH)

    with open(CANDIDATE_IDS_PATH, "r") as file:
        candidate_ids = json.load(file)
else:
    index = create_index()
    candidate_ids = []


class AnnSearchRequest(BaseModel):
    embedding: list[float]


class AddEmbeddingRequest(BaseModel):
    candidate_id: str
    embedding: list[float]


class RebuildIndexRequest(BaseModel):
    embeddings: list[list[float]]
    candidate_ids: list[str]


@app.post("/generate-embedding")
async def generate_embedding(
    image: UploadFile = File(...)
):
    if (
        not image.content_type
        or not image.content_type.startswith("image/")
    ):
        raise HTTPException(
            status_code=400,
            detail="Uploaded file must be an image."
        )

    image_bytes = await image.read()

    try:
        img = Image.open(
            io.BytesIO(image_bytes)
        ).convert("RGB")
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


@app.get("/index-status")
async def index_status():
    return {
        "total_embeddings": index.ntotal
    }


@app.post("/add-embedding")
async def add_embedding(
    request: AddEmbeddingRequest
):
    embedding = np.array(
        [request.embedding],
        dtype=np.float32
    )

    if request.candidate_id in candidate_ids:
        raise HTTPException(
            status_code=400,
            detail="This candidate already exists in the ANN index."
        )

    if embedding.shape[1] != EMBEDDING_DIMENSION:
        raise HTTPException(
            status_code=400,
            detail="Embedding must have 512 dimensions."
        )

    faiss.normalize_L2(embedding)

    index.add(embedding)
    candidate_ids.append(request.candidate_id)

    save_index()

    return {
        "message": "Embedding added to ANN index."
    }


@app.post("/search-candidates")
async def search_candidates(
    request: AnnSearchRequest
):
    if index.ntotal == 0:
        return {
            "candidate_ids": []
        }

    query = np.array(
        [request.embedding],
        dtype=np.float32
    )

    if query.shape[1] != EMBEDDING_DIMENSION:
        raise HTTPException(
            status_code=400,
            detail="Embedding must have 512 dimensions."
        )

    faiss.normalize_L2(query)

    k = min(5, index.ntotal)

    distances, indices = index.search(
        query,
        k
    )

    matching_candidate_ids = [
        candidate_ids[i]
        for i in indices[0]
        if i != -1
    ]

    return {
        "candidate_ids": matching_candidate_ids
    }


@app.post("/rebuild-index")
async def rebuild_index(
    request: RebuildIndexRequest
):
    global index, candidate_ids

    if len(request.embeddings) != len(
        request.candidate_ids
    ):
        raise HTTPException(
            status_code=400,
            detail="Embeddings and Candidate IDs must have the same length."
        )

    # Create a completely fresh index.
    index = create_index()
    candidate_ids = []

    # Empty database is valid.
    if len(request.embeddings) == 0:
        save_index()

        return {
            "message": "ANN index rebuilt successfully.",
            "total_embeddings": 0
        }

    embeddings = np.array(
        request.embeddings,
        dtype=np.float32
    )

    if (
        embeddings.ndim != 2
        or embeddings.shape[1] != EMBEDDING_DIMENSION
    ):
        raise HTTPException(
            status_code=400,
            detail="All embeddings must have 512 dimensions."
        )

    faiss.normalize_L2(embeddings)

    index.add(embeddings)

    candidate_ids = request.candidate_ids.copy()

    save_index()

    return {
        "message": "ANN index rebuilt successfully.",
        "total_embeddings": index.ntotal
    }