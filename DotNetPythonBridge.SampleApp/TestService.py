import argparse
import uvicorn
from fastapi import FastAPI, BackgroundTasks
from fastapi.responses import JSONResponse
import os
import signal

app = FastAPI()

def shutdown_server():
    os.kill(os.getpid(), signal.SIGTERM)

@app.get("/shutdown")
async def shutdown(background_tasks: BackgroundTasks):
    background_tasks.add_task(shutdown_server)
    return JSONResponse(content={"message": "Shutting down..."}, status_code=200)

@app.get("/health")
def health():
    return {"status": "ok"}

@app.get("/")
def root():
    return {"message": "Hello from Python Service!"}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1", help="Host address")
    parser.add_argument("--port", type=int, default=8000, help="Port number")
    args, _ = parser.parse_known_args()

    uvicorn.run(app, host=args.host, port=args.port)

if __name__ == "__main__":
    main()