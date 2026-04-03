#!/usr/bin/env python3
"""
TS Laser CRM startup script.
"""

import os
import uvicorn


if __name__ == "__main__":
    os.chdir(os.path.dirname(os.path.abspath(__file__)))

    print("=" * 50)
    print("TS LASER CRM")
    print("=" * 50)
    print("Open in browser: http://localhost:8000")
    print("Default password: tslaser2026")
    print("Press Ctrl+C to stop")
    print("=" * 50)

    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
    )
