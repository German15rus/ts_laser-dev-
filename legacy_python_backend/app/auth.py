from fastapi import Request, HTTPException, status
from itsdangerous import URLSafeTimedSerializer, BadSignature, SignatureExpired
import os
import hashlib

# Simple password for authentication
# In production, use environment variable: os.environ.get("CRM_PASSWORD", "tslaser2026")
CRM_PASSWORD = "tslaser2026"

# Secret key for session signing
SECRET_KEY = os.environ.get("SECRET_KEY", "tslaser-secret-key-change-in-production")
SESSION_COOKIE_NAME = "tslaser_session"
SESSION_MAX_AGE = 60 * 60 * 24 * 7  # 7 days

serializer = URLSafeTimedSerializer(SECRET_KEY)


def hash_password(password: str) -> str:
    """Hash password using SHA256"""
    return hashlib.sha256(password.encode()).hexdigest()


def verify_password(password: str) -> bool:
    """Verify if password matches"""
    return password == CRM_PASSWORD


def create_session_token() -> str:
    """Create a signed session token"""
    return serializer.dumps({"authenticated": True})


def verify_session_token(token: str) -> bool:
    """Verify session token"""
    try:
        data = serializer.loads(token, max_age=SESSION_MAX_AGE)
        return data.get("authenticated", False)
    except (BadSignature, SignatureExpired):
        return False


def get_current_user(request: Request) -> bool:
    """Check if user is authenticated via session cookie"""
    token = request.cookies.get(SESSION_COOKIE_NAME)
    if not token:
        return False
    return verify_session_token(token)


def require_auth(request: Request):
    """Dependency to require authentication"""
    if not get_current_user(request):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Not authenticated"
        )
    return True
